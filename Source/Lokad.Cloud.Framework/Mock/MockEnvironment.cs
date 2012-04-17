using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace Lokad.Cloud.Mock
{
    public class MockEnvironment : IEnvironment
    {
        public HostInfo Host
        {
            get { return new HostInfo("MockWorker", "MockCell", "MockSolution"); }
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
            return Path.Combine(Path.GetTempPath(), "LokadCloudMock");
        }

        public IPEndPoint GetEndpoint(string endpointName)
        {
            return null;
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
    }
}
