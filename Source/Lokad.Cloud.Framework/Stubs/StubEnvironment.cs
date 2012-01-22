using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.AppHost.Framework.Definition;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Lokad.Cloud.Stubs
{
    public class StubEnvironment : IApplicationEnvironment
    {
        private readonly string _instance = Guid.NewGuid().ToString("N");

        public StubEnvironment()
        {
            Deployment = new SolutionHead("Solution");
            Host = new HostLifeIdentity("MockHost", "MockHost-" + _instance);
            Cell = new CellLifeIdentity(Host, "Solution", "Cell", "Cell" + _instance);
            Assemblies = new AssembliesHead("Assemblies");
        }

        public SolutionHead Deployment { get; private set; }
        public HostLifeIdentity Host { get; private set; }
        public CellLifeIdentity Cell { get; private set; }
        public AssembliesHead Assemblies { get; private set; }

        public int CurrentWorkerInstanceCount
        {
            get { return 1; }
        }

        public void LoadDeployment(SolutionHead deployment)
        {
        }

        public void LoadCurrentHeadDeployment()
        {
        }

        public void ProvisionWorkerInstances(int numberOfInstances)
        {
        }

        public void ProvisionWorkerInstancesAtLeast(int minNumberOfInstances)
        {
        }

        public string GetSettingValue(string settingName)
        {
            return RoleEnvironment.GetConfigurationSettingValue(settingName);
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

        public void SendCommand(IHostCommand command)
        {
        }
    }
}
