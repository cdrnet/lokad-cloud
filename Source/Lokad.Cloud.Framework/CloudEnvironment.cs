#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Lokad.Cloud.Management;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Lokad.Cloud
{
    /// <summary>
    /// Cloud Environment
    /// </summary>
    /// <remarks>
    /// See also AppHost IApplicationEnvironment
    /// </remarks>
    public sealed class CloudEnvironment : ICloudEnvironment
    {
        static bool _runtimeAvailable;
        private readonly IProvisioningProvider _provisioning;
        private readonly Lazy<string> _hostName = new Lazy<string>(Dns.GetHostName);
        private readonly string _uniqueInstanceName;

        public CloudEnvironment(IProvisioningProvider provisioning, string uniqueInstanceName)
        {
            _provisioning = provisioning;
            _uniqueInstanceName = uniqueInstanceName;
        }

        string ICloudEnvironment.WorkerName
        {
            get { return _hostName.Value; }
        }

        string ICloudEnvironment.UniqueWorkerInstanceName
        {
            get { return _uniqueInstanceName; }
        }

        int ICloudEnvironment.CurrentWorkerInstanceCount
        {
            get
            {
                return _provisioning.GetWorkerInstanceCount(CancellationToken.None).Result;
            }
        }

        void ICloudEnvironment.ProvisionWorkerInstances(int numberOfInstances)
        {
            _provisioning.SetWorkerInstanceCount(numberOfInstances, CancellationToken.None);
        }

        void ICloudEnvironment.ProvisionWorkerInstancesAtLeast(int minNumberOfInstances)
        {
            _provisioning.GetWorkerInstanceCount(CancellationToken.None).ContinueWith(task =>
                {
                    if (task.Result < minNumberOfInstances)
                    {
                        _provisioning.SetWorkerInstanceCount(minNumberOfInstances, CancellationToken.None);
                    }
                });
        }

        string ICloudEnvironment.GetSettingValue(string settingName)
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

        X509Certificate2 ICloudEnvironment.GetCertificate(string thumbprint)
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

        string ICloudEnvironment.GetLocalResourcePath(string resourceName)
        {
            if (IsAvailable)
            {
                return RoleEnvironment.GetLocalResource(resourceName).RootPath;
            }

            var dir = Path.Combine(Path.GetTempPath(), resourceName);
            Directory.CreateDirectory(dir);
            return dir;
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