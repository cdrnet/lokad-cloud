using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace Lokad.Cloud.Stubs
{
    public class StubEnvironment : IEnvironment
    {
        private readonly string _instance = Guid.NewGuid().ToString("N");

        public StubEnvironment()
        {
            Host = new HostInfo("HostStub", "Cell", "Solution");
        }

        public HostInfo Host { get; private set; }

        public void LoadNewestDeploymentIfChanged()
        {
        }

        public int CurrentWorkerInstanceCount
        {
            get { return 1; }
        }

        public void ProvisionWorkerInstances(int numberOfInstances)
        {
        }

        public void ProvisionWorkerInstancesAtLeast(int minNumberOfInstances)
        {
        }

        public string GetSettingValue(string settingName)
        {
            return null;
        }

        public X509Certificate2 GetCertificate(string thumbprint)
        {
            return null;
        }

        public string GetLocalResourcePath(string resourceName)
        {
            return Path.Combine(Path.GetTempPath(), _instance, resourceName);
        }

        public IPEndPoint GetEndpoint(string endpointName)
        {
            return null;
        }
    }
}
