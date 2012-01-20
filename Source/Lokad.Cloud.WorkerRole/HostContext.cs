#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.AppHost.Framework.Definition;
using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.Provisioning;
using Lokad.Cloud.Provisioning.Instrumentation;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Lokad.Cloud
{
    internal class HostContext : IHostContext
    {
        static bool _runtimeAvailable;

        private readonly HostLogWriter _log;
        private readonly HostLifeIdentity _identity;
        private readonly AzureCurrentDeployment _currentDeployment;
        private readonly AzureProvisioning _provisioning;

        public HostContext(IDeploymentReader deploymentReader, string certificateThumbprint, string subscriptionId, HostLogWriter log, IHostObserver observer, ICloudProvisioningObserver provisioningObserver)
        {
            Observer = observer;
            DeploymentReader = deploymentReader;
            _log = log;

            // TODO: Replace GUID with global blob counter
            _identity = new HostLifeIdentity(Environment.MachineName, Guid.NewGuid().ToString("N"));

            // try get settings and certificate
            if (!IsAvailable)
            {
                log.Log(HostLogLevel.Warn, string.Format("Provisioning: RoleEnvironment not available on worker {0}.", _identity.WorkerName));
                return;
            }

            try
            {
                if (!RoleEnvironment.IsAvailable)
                {

                    return;
                }
            }
            catch (TypeInitializationException)
            {
                log.Log(HostLogLevel.Warn, string.Format("Provisioning: RoleEnvironment not available on worker {0}.", _identity.WorkerName));
                return;
            }

            var currentDeploymentPrivateId = RoleEnvironment.DeploymentId;
            X509Certificate2 certificate = null;
            if (!String.IsNullOrWhiteSpace(certificateThumbprint))
            {
                certificate = GetCertificate(certificateThumbprint);
            }

            // early evaluate management status for intrinsic fault states, to skip further processing
            if (currentDeploymentPrivateId == null || certificate == null || string.IsNullOrWhiteSpace(subscriptionId))
            {
                log.Log(HostLogLevel.Debug, "Provisioning: Not available because either the certificate or the subscription was not provided correctly.");
                return;
            }

            // detect dev fabric
            if (currentDeploymentPrivateId.StartsWith("deployment("))
            {
                log.Log(HostLogLevel.Debug, "Provisioning: Not available in dev fabric instance.");
                return;
            }

            // ok
            _provisioning = new AzureProvisioning(subscriptionId, certificate, provisioningObserver);
            _currentDeployment = new AzureCurrentDeployment(currentDeploymentPrivateId, subscriptionId, certificate, provisioningObserver);
        }

        public HostLifeIdentity Identity
        {
            get { return _identity; }
        }

        public CellLifeIdentity GetNewCellLifeIdentity(string solutionName, string cellName, SolutionHead deployment)
        {
            // TODO: Replace GUID with global blob counter
            return new CellLifeIdentity(_identity, solutionName, cellName, Guid.NewGuid().ToString("N"));
        }

        public string GetSettingValue(string settingName)
        {
            if (!IsAvailable)
            {
                return null;
            }

            try
            {
                var value = RoleEnvironment.GetConfigurationSettingValue(settingName);
                if (!String.IsNullOrEmpty(value))
                {
                    value = value.Trim();
                }

                return String.IsNullOrEmpty(value) ? null : value;
            }
            catch (RoleEnvironmentException)
            {
                // e.g. setting was removed from the csdef
                return null;
            }
        }

        public X509Certificate2 GetCertificate(string thumbprint)
        {
            var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            try
            {
                store.Open(OpenFlags.ReadOnly);
                var certs = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);
                if (certs.Count != 1)
                {
                    return null;
                }

                return certs[0];
            }
            finally
            {
                store.Close();
            }
        }

        public string GetLocalResourcePath(string resourceName)
        {
            if (IsAvailable)
            {
                return RoleEnvironment.GetLocalResource(resourceName).RootPath;
            }

            var dir = Path.Combine(Path.GetTempPath(), _identity.UniqueWorkerInstanceName, resourceName);
            Directory.CreateDirectory(dir);
            return dir;
        }

        public IPEndPoint GetEndpoint(string endpointName)
        {
            if (!IsAvailable)
            {
                return null;
            }

            RoleInstanceEndpoint endpoint;
            return RoleEnvironment.CurrentRoleInstance.InstanceEndpoints.TryGetValue(endpointName, out endpoint) ? endpoint.IPEndpoint : null;
        }

        public int CurrentWorkerInstanceCount
        {
            get { return GetWorkerInstanceCount(CancellationToken.None).Result; }
        }

        public void ProvisionWorkerInstances(int numberOfInstances)
        {
            SetWorkerInstanceCount(numberOfInstances, CancellationToken.None);
        }

        public void ProvisionWorkerInstancesAtLeast(int minNumberOfInstances)
        {
            GetWorkerInstanceCount(CancellationToken.None).ContinueWith(task =>
                {
                    if (task.Result < minNumberOfInstances)
                    {
                        SetWorkerInstanceCount(minNumberOfInstances, CancellationToken.None);
                    }
                });
        }

        public IDeploymentReader DeploymentReader { get; private set; }

        public IHostObserver Observer { get; private set; }

        /// <remarks>
        /// Logs exceptions, hence failing to handle a task fault at the calling side
        /// will not cause an unhandled exception at finalization
        /// </remarks>
        private Task<int> GetWorkerInstanceCount(CancellationToken cancellationToken)
        {
            var task = _provisioning.GetCurrentLokadCloudWorkerCount(_currentDeployment, cancellationToken);

            // TODO (ruegg, 2011-05-30): Replace with system events
            task.ContinueWith(t =>
            {
                try
                {
                    if (t.IsFaulted)
                    {
                        if (ProvisioningErrorHandling.IsTransientError(t.Exception))
                        {
                            _log.Log(HostLogLevel.Debug, task.Exception.GetBaseException(), "Provisioning: Getting the current worker instance count failed with a transient error.");
                        }
                        else
                        {
                            _log.Log(HostLogLevel.Warn, task.Exception.GetBaseException(), "Provisioning: Getting the current worker instance count failed with a permanent error.");
                        }
                    }
                }
                catch (Exception)
                {
                    // We don't really care, it's only logging that failed
                }
            }, TaskContinuationOptions.ExecuteSynchronously);

            return task;
        }

        /// <remarks>
        /// Logs exceptions, hence failing to handle a task fault at the calling side
        /// will not cause an unhandled exception at finalization
        /// </remarks>
        private Task SetWorkerInstanceCount(int count, CancellationToken cancellationToken)
        {
            if (count <= 0 && count > 500)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            _log.Log(HostLogLevel.Info, string.Format("Provisioning: Updating the worker instance count to {0}.", count));

            var task = _provisioning.UpdateCurrentLokadCloudWorkerCount(_currentDeployment, count, cancellationToken);

            // TODO (ruegg, 2011-05-30): Replace with system events
            task.ContinueWith(t =>
            {
                try
                {
                    if (t.IsFaulted)
                    {
                        HttpStatusCode httpStatus;
                        if (ProvisioningErrorHandling.TryGetHttpStatusCode(t.Exception, out httpStatus))
                        {
                            switch (httpStatus)
                            {
                                case HttpStatusCode.Conflict:
                                    _log.Log(HostLogLevel.Debug, string.Format("Provisioning: Updating the worker instance count to {0} failed because another deployment update is already in progress.", count));
                                    break;
                                default:
                                    _log.Log(HostLogLevel.Debug, string.Format("Provisioning: Updating the worker instance count failed with HTTP Status {0} ({1}).", httpStatus));
                                    break;
                            }
                        }
                        else if (ProvisioningErrorHandling.IsTransientError(t.Exception))
                        {
                            _log.Log(HostLogLevel.Debug, task.Exception.GetBaseException(), "Provisioning: Updating the worker instance count failed with a transient error.");
                        }
                        else
                        {
                            _log.Log(HostLogLevel.Warn, task.Exception.GetBaseException(), "Provisioning: Updating the worker instance count failed with a permanent error.");
                        }
                    }
                }
                catch (Exception)
                {
                    // We don't really care, it's only logging that failed
                }
            }, TaskContinuationOptions.ExecuteSynchronously);

            return task;
        }

        static bool IsAvailable
        {
            get
            {
                if (!_runtimeAvailable)
                {
                    // try again, maybe it was not available the last time but now is.
                    try
                    {
                        _runtimeAvailable = RoleEnvironment.IsAvailable;
                    }
                    catch (TypeInitializationException)
                    {
                        _runtimeAvailable = false;
                    }
                }

                return _runtimeAvailable;
            }
        }
    }
}
