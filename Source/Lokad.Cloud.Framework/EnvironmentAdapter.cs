#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.Provisioning;
using Lokad.Cloud.Provisioning.Instrumentation;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Lokad.Cloud
{
    /// <summary>
    /// Intermediate CloudEnvironment to IEnvironment adapter.
    /// </summary>
    [Obsolete("To be removed after the transition away from statis CloudEnvironment is completed.")]
    public class EnvironmentAdapter : MarshalByRefObject, IEnvironment
    {
        static bool _runtimeAvailable;
        readonly Lazy<string> _hostName = new Lazy<string>(Dns.GetHostName);

        private readonly ILog _log;
        private readonly AzureCurrentDeployment _currentDeployment;
        private readonly AzureProvisioning _provisioning;

        public EnvironmentAdapter(ICloudConfigurationSettings settings, ILog log, IProvisioningObserver provisioningObserver = null)
        {
            _log = log;

            // try get settings and certificate
            if (!IsAvailable)
            {
                _log.WarnFormat("Provisioning: RoleEnvironment not available on worker {0}.", _hostName.Value);
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
                _log.WarnFormat("Provisioning: RoleEnvironment not available on worker {0}.", _hostName.Value);
                return;
            }

            var currentDeploymentPrivateId = RoleEnvironment.DeploymentId;
            X509Certificate2 certificate = null;
            if (!String.IsNullOrWhiteSpace(settings.SelfManagementCertificateThumbprint))
            {
                certificate = GetCertificate(settings.SelfManagementCertificateThumbprint);
            }

            // early evaluate management status for intrinsic fault states, to skip further processing
            if (currentDeploymentPrivateId == null || certificate == null || string.IsNullOrWhiteSpace(settings.SelfManagementSubscriptionId))
            {
                _log.DebugFormat("Provisioning: Not available because either the certificate or the subscription was not provided correctly.");
                return;
            }

            // detect dev fabric
            if (currentDeploymentPrivateId.StartsWith("deployment("))
            {
                _log.Debug("Provisioning: Not available in dev fabric instance.");
                return;
            }

            // ok
            _provisioning = new AzureProvisioning(settings.SelfManagementSubscriptionId, certificate, provisioningObserver);
            _currentDeployment = new AzureCurrentDeployment(currentDeploymentPrivateId, settings.SelfManagementSubscriptionId, certificate, provisioningObserver);
        }

        public HostInfo Host
        {
            get { return new HostInfo(_hostName.Value, "Primary", "Lokad.Cloud"); }
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
                return store.Certificates
                    .Find(X509FindType.FindByThumbprint, thumbprint, false)
                    .OfType<X509Certificate2>()
                    .FirstOrDefault();
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

            var dir = Path.Combine(Path.GetTempPath(), _hostName.Value, resourceName);
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
            get
            {
                return GetWorkerInstanceCount(CancellationToken.None).Result;
            }
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

        /// <remarks>
        /// Logs exceptions, hence failing to handle a task fault at the calling side
        /// will not cause an unhandled exception at finalization
        /// </remarks>
        Task<int> GetWorkerInstanceCount(CancellationToken cancellationToken)
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
                            _log.DebugFormat(task.Exception.GetBaseException(), "Provisioning: Getting the current worker instance count failed with a transient error.");
                        }
                        else
                        {
                            _log.WarnFormat(task.Exception.GetBaseException(), "Provisioning: Getting the current worker instance count failed with a permanent error.");
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
        Task SetWorkerInstanceCount(int count, CancellationToken cancellationToken)
        {
            if (count <= 0 && count > 500)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            _log.InfoFormat("Provisioning: Updating the worker instance count to {0}.", count);

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
                                    _log.DebugFormat("Provisioning: Updating the worker instance count to {0} failed because another deployment update is already in progress.", count);
                                    break;
                                default:
                                    _log.DebugFormat("Provisioning: Updating the worker instance count failed with HTTP Status {0} ({1}).", httpStatus, (int)httpStatus);
                                    break;
                            }
                        }
                        else if (ProvisioningErrorHandling.IsTransientError(t.Exception))
                        {
                            _log.DebugFormat(task.Exception.GetBaseException(), "Provisioning: Updating the worker instance count failed with a transient error.");
                        }
                        else
                        {
                            _log.WarnFormat(task.Exception.GetBaseException(), "Provisioning: Updating the worker instance count failed with a permanent error.");
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
