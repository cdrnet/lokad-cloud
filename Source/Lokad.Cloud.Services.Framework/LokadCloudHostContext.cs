using System;
using System.Security.Cryptography.X509Certificates;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.Storage;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Lokad.Cloud.Services.Framework
{
    [Serializable]
    public class LokadCloudHostContext : IHostContext
    {
        public string AzureDeploymentId { get; protected set; }
        public string DataConnectionString { get; protected set; }
        public string SelfManagementSubscriptionId { get; protected set; }
        public IDeploymentReader DeploymentReader { get; protected set; }
        public X509Certificate2 SelfManagementCertificate { get; protected set; }

        public CloudStorageProviders BuildStorage()
        {
            return CloudStorage.ForAzureConnectionString(DataConnectionString).BuildStorageProviders();
        }

        public int CurrentWorkerInstanceCount
        {
            get { return SafeGet(() => RoleEnvironment.CurrentRoleInstance.Role.Instances.Count); }
        }

        public string GetConfigurationSettingValue(string settingName)
        {
            return SafeGet(() => RoleEnvironment.GetConfigurationSettingValue(settingName));
        }

        public X509Certificate2 GetConfigurationCertificate(string thumbprint)
        {
            return GetCertificate(thumbprint);
        }

        public static LokadCloudHostContext CreateFromRoleEnvironment()
        {
            var context = new LokadCloudHostContext
            {
                AzureDeploymentId = SafeGet(() => RoleEnvironment.DeploymentId),
                DataConnectionString = SafeGet(() => RoleEnvironment.GetConfigurationSettingValue("DataConnectionString")),
                SelfManagementSubscriptionId = SafeGet(() => RoleEnvironment.GetConfigurationSettingValue("SelfManagementSubscriptionId"))
            };

            var thumbprint = SafeGet(() => RoleEnvironment.GetConfigurationSettingValue("SelfManagementCertificateThumbprint"));
            if (thumbprint != null)
            {
                context.SelfManagementCertificate = SafeGet(() => GetCertificate(thumbprint));
            }

            context.DeploymentReader = new LokadCloudDeploymentReader(context.DataConnectionString);

            return context;
        }

        static  X509Certificate2 GetCertificate(string thumbprint)
        {
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
