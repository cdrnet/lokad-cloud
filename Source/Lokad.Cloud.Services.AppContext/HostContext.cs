#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.Provisioning;
using Lokad.Cloud.Provisioning.Instrumentation;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Lokad.Cloud.Services.AppContext
{
    public class HostContext : IHostContext
    {
        readonly Lazy<AzureCurrentDeployment> _currentDeployment;
        readonly Lazy<AzureProvisioning> _provisioning;

        public string DataConnectionString { get; private set; }
        public IDeploymentReader DeploymentReader { get; private set; }

        // Optional:
        public IHostObserver Observer { get; set; }
        public ICloudProvisioningObserver ProvisioningObserver { get; set; }

        public HostContext()
        {
            DataConnectionString = SafeGet(() => RoleEnvironment.GetConfigurationSettingValue("DataConnectionString"));
            DeploymentReader = new DeploymentReader(DataConnectionString);

            var deploymentId = SafeGet(() => RoleEnvironment.DeploymentId);
            var subscriptionId = SafeGet(() => RoleEnvironment.GetConfigurationSettingValue("SelfManagementSubscriptionId"));
            var certificate = SafeGet(() => GetCertificateInternal(RoleEnvironment.GetConfigurationSettingValue("SelfManagementCertificateThumbprint")));

            if (!string.IsNullOrWhiteSpace(deploymentId) && !deploymentId.StartsWith("deployment(")
                && !string.IsNullOrWhiteSpace(subscriptionId) && certificate != null)
            {
                _currentDeployment = new Lazy<AzureCurrentDeployment>(() => new AzureCurrentDeployment(
                    deploymentId, subscriptionId, certificate, ProvisioningObserver));

                _provisioning = new Lazy<AzureProvisioning>(() => new AzureProvisioning(
                    subscriptionId, certificate, ProvisioningObserver));
            }
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
            if (numberOfInstances <= 0 && numberOfInstances > 500)
            {
                throw new ArgumentOutOfRangeException("numberOfInstances");
            }

            if (_provisioning == null)
            {
                return;
            }

            var currentDeployment = _currentDeployment.Value;
            var provisioning = _provisioning.Value;

            provisioning.UpdateCurrentLokadCloudWorkerCount(currentDeployment, numberOfInstances, CancellationToken.None);
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
