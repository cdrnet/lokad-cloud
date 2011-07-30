#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.Storage;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Lokad.Cloud.Services.App
{
    [Serializable]
    public class HostContext : IHostContext
    {
        public string AzureDeploymentId { get; protected set; }
        public string DataConnectionString { get; protected set; }
        public string SelfManagementSubscriptionId { get; protected set; }
        public IDeploymentReader DeploymentReader { get; protected set; }
        public X509Certificate2 SelfManagementCertificate { get; protected set; }

        public IHostObserver Observer { get; set; }

        public CloudStorageProviders BuildStorage()
        {
            return CloudStorage.ForAzureConnectionString(DataConnectionString).BuildStorageProviders();
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
            throw new NotImplementedException();
        }

        public void ProvisionWorkerInstancesAtLeast(int minNumberOfInstances)
        {
            throw new NotImplementedException();
        }

        public static HostContext CreateFromRoleEnvironment()
        {
            var context = new HostContext
            {
                AzureDeploymentId = SafeGet(() => RoleEnvironment.DeploymentId),
                DataConnectionString = SafeGet(() => RoleEnvironment.GetConfigurationSettingValue("DataConnectionString")),
                SelfManagementSubscriptionId = SafeGet(() => RoleEnvironment.GetConfigurationSettingValue("SelfManagementSubscriptionId"))
            };

            var thumbprint = SafeGet(() => RoleEnvironment.GetConfigurationSettingValue("SelfManagementCertificateThumbprint"));
            if (thumbprint != null)
            {
                context.SelfManagementCertificate = SafeGet(() => GetCertificateInternal(thumbprint));
            }

            context.DeploymentReader = new DeploymentReader(context.DataConnectionString);

            return context;
        }

        static X509Certificate2 GetCertificateInternal(string thumbprint)
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
