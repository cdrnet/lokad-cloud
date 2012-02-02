#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Net;
using System.Security.Cryptography.X509Certificates;
using Lokad.Cloud.AppHost.Framework;

namespace Lokad.Cloud.Services.EntryPoint
{
    public class EnvironmentAdapter : IEnvironment
    {
        private readonly IApplicationEnvironment _environment;

        public EnvironmentAdapter(IApplicationEnvironment environment)
        {
            _environment = environment;
        }

        public HostInfo Host
        {
            get { return new HostInfo(_environment.Host.WorkerName, _environment.Cell.CellName, _environment.Cell.SolutionName); }
        }

        public void LoadNewestDeploymentIfChanged()
        {
            _environment.LoadCurrentHeadDeployment();
        }

        public int CurrentWorkerInstanceCount
        {
            get { return _environment.CurrentWorkerInstanceCount; }
        }

        public void ProvisionWorkerInstances(int numberOfInstances)
        {
            _environment.ProvisionWorkerInstances(numberOfInstances);
        }

        public void ProvisionWorkerInstancesAtLeast(int minNumberOfInstances)
        {
            _environment.ProvisionWorkerInstancesAtLeast(minNumberOfInstances);
        }

        public string GetSettingValue(string settingName)
        {
            return _environment.GetSettingValue(settingName);
        }

        public X509Certificate2 GetCertificate(string thumbprint)
        {
            return _environment.GetCertificate(thumbprint);
        }

        public string GetLocalResourcePath(string resourceName)
        {
            return _environment.GetLocalResourcePath(resourceName);
        }

        public IPEndPoint GetEndpoint(string endpointName)
        {
            return _environment.GetEndpoint(endpointName);
        }
    }
}
