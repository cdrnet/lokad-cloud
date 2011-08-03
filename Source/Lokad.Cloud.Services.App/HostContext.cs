#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.Provisioning.Instrumentation;
using Lokad.Cloud.Services.Framework.Logging;
using Lokad.Cloud.Storage;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Lokad.Cloud.Services.App
{
    public class HostContext : IHostContext
    {
        readonly string _dataConnectionString;
        readonly Lazy<CloudProvisioning> _provisioning;

        public IDeploymentReader DeploymentReader { get; protected set; }

        // Optional:
        // TODO: Get rid of direct logging (use system events instead)
        public ILogWriter Log { get; set; }
        public IHostObserver Observer { get; set; }
        public ICloudProvisioningObserver ProvisioningObserver { get; set; }

        public HostContext()
        {
            _dataConnectionString = SafeGet(() => RoleEnvironment.GetConfigurationSettingValue("DataConnectionString"));

            _provisioning = new Lazy<CloudProvisioning>(() => new CloudProvisioning(
                SafeGet(() => RoleEnvironment.GetConfigurationSettingValue("SelfManagementSubscriptionId")),
                SafeGet(() => RoleEnvironment.DeploymentId),
                SafeGet(() => GetCertificateInternal(RoleEnvironment.GetConfigurationSettingValue("SelfManagementCertificateThumbprint"))),
                Dns.GetHostName(),
                Log,
                ProvisioningObserver));

            var logStorage = CloudStorage.ForAzureConnectionString(_dataConnectionString).BuildStorageProviders();
            Log = new CloudLogWriter(logStorage);

            // we have to pass the connection string instead of storage providers because
            // DeploymentReader must be serializable (to cross AppDomains)
            DeploymentReader = new DeploymentReader(_dataConnectionString);
        }

        public string GetSettingValue(string settingName)
        {
            return SafeGet(() => RoleEnvironment.GetConfigurationSettingValue(settingName));
        }

        public X509Certificate2 GetCertificate(string thumbprint)
        {
            return GetCertificateInternal(thumbprint);
        }

        public string GetLocalResourcePath(string resourceName)
        {
            var path = SafeGet(() => RoleEnvironment.GetLocalResource(resourceName).RootPath);
            if (path == null)
            {
                path = Path.Combine(Path.GetTempPath(), resourceName);
                Directory.CreateDirectory(path);
            }

            return path;
        }

        public int CurrentWorkerInstanceCount
        {
            get { return SafeGet(() => RoleEnvironment.CurrentRoleInstance.Role.Instances.Count); }
        }

        public void ProvisionWorkerInstances(int numberOfInstances)
        {
            var provisioning = _provisioning.Value;
            if (!provisioning.IsAvailable)
            {
                return;
            }

            // faulted case is already handled and logged by CloudProvisioning
            // TODO: cleanup
            provisioning.SetWorkerInstanceCount(numberOfInstances, CancellationToken.None);
        }

        public void ProvisionWorkerInstancesAtLeast(int minNumberOfInstances)
        {
            if (CurrentWorkerInstanceCount >= minNumberOfInstances)
            {
                return;
            }

            ProvisionWorkerInstances(minNumberOfInstances);
        }

        static X509Certificate2 GetCertificateInternal(string thumbprint)
        {
            if (string.IsNullOrWhiteSpace(thumbprint))
            {
                return null;
            }

            var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            try
            {
                store.Open(OpenFlags.ReadOnly);
                var certs = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);
                return certs.Count != 1 ? null : certs[0];
            }
            finally
            {
                store.Close();
            }
        }

        static T SafeGet<T>(Func<T> getter)
        {
            // dirty, yet protecting us from unreliable RoleEnvironment and missing
            // config entries. Also, we need to check for empty/default values anyway,
            // but where missing and inexistent values are not treated differently.

            try
            {
                return getter();
            }
            catch (RoleEnvironmentException)
            {
                // setting was removed from the csdef, skip
                return default(T);
            }
            catch (TypeInitializationException)
            {
                // azure runtime not available, nothing we can do abour
                return default(T);
            }
        }
    }
}
