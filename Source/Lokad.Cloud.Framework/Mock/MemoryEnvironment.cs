using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Lokad.Cloud.Mock
{
    public class MemoryEnvironment : ICloudEnvironment
    {
        private readonly string instance = Guid.NewGuid().ToString("N");

        string ICloudEnvironment.WorkerName
        {
            get { return "MockHost"; }
        }

        string ICloudEnvironment.UniqueWorkerInstanceName
        {
            get { return "MockHost-" + instance; }
        }

        int ICloudEnvironment.CurrentWorkerInstanceCount
        {
            get { return 1; }
        }

        void ICloudEnvironment.ProvisionWorkerInstances(int numberOfInstances)
        {
        }

        void ICloudEnvironment.ProvisionWorkerInstancesAtLeast(int minNumberOfInstances)
        {
        }

        string ICloudEnvironment.GetSettingValue(string settingName)
        {
            return RoleEnvironment.GetConfigurationSettingValue(settingName);
        }

        X509Certificate2 ICloudEnvironment.GetCertificate(string thumbprint)
        {
            return null;
        }

        string ICloudEnvironment.GetLocalResourcePath(string resourceName)
        {
            return Path.Combine(Path.GetTempPath(), instance, resourceName);
        }
    }
}
