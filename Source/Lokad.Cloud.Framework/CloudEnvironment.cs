#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.Management;
using Lokad.Cloud.Provisioning;
using Lokad.Cloud.Provisioning.Instrumentation;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Lokad.Cloud
{
    /// <summary>
    /// Cloud Environment
    /// </summary>
    /// <remarks>
    /// See also AppHost IApplicationEnvironment
    /// </remarks>
    public sealed class CloudEnvironment : ICloudEnvironment, IProvisioningProvider
    {
        static bool _runtimeAvailable;
        private readonly Lazy<string> _hostName = new Lazy<string>(Dns.GetHostName);
        private readonly string _uniqueInstanceName;

        private readonly ILog _log;
        private readonly AzureCurrentDeployment _currentDeployment;
        private readonly AzureProvisioning _provisioning;

        public CloudEnvironment(string uniqueInstanceName, CloudConfigurationSettings settings, ILog log, ICloudProvisioningObserver provisioningObserver = null)
        {
            _uniqueInstanceName = uniqueInstanceName;
            _log = log;

            // try get settings and certificate
            if (!IsAvailable)
            {
                _log.WarnFormat("Provisioning: RoleEnvironment not available on worker {0}.", WorkerName);
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
                _log.WarnFormat("Provisioning: RoleEnvironment not available on worker {0}.", WorkerName);
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

        public string WorkerName
        {
            get { return _hostName.Value; }
        }

       public string UniqueWorkerInstanceName
        {
            get { return _uniqueInstanceName; }
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
                return null;
                // setting was removed from the csdef, skip
                // (logging is usually not available at that stage)
            }
        }

        public X509Certificate2 GetCertificate(string thumbprint)
        {
            var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            try
            {
                store.Open(OpenFlags.ReadOnly);
                var certs = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);
                if(certs.Count != 1)
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

            var dir = Path.Combine(Path.GetTempPath(), resourceName);
            Directory.CreateDirectory(dir);
            return dir;
        }

        bool IProvisioningProvider.IsAvailable
        {
            get { return _provisioning != null; }
        }

        /// <remarks>
        /// Logs exceptions, hence failing to handle a task fault at the calling side
        /// will not cause an unhandled exception at finalization
        /// </remarks>
        public Task<int> GetWorkerInstanceCount(CancellationToken cancellationToken)
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
        public Task SetWorkerInstanceCount(int count, CancellationToken cancellationToken)
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

        /// <summary>
        /// Indicates whether the instance is running in the Cloud environment.
        /// </summary>
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