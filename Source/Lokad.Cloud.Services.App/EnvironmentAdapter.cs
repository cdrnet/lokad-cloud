#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Security.Cryptography.X509Certificates;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.Services.Framework;

namespace Lokad.Cloud.Services.App
{
    /// <summary>
    /// Adapting AppHost IApplicationEnvironment to Services ICloudEnvornment
    /// (bridge to avoid dependencies)
    /// </summary>
    internal class EnvironmentAdapter : ICloudEnvironment
    {
        private readonly IApplicationEnvironment _inner;

        public EnvironmentAdapter(IApplicationEnvironment inner)
        {
            _inner = inner;
        }


        string ICloudEnvironment.MachineName
        {
            get { return _inner.MachineName; }
        }

        string ICloudEnvironment.CellName
        {
            get { return _inner.CellName; }
        }


        string ICloudEnvironment.CurrentDeploymentName
        {
            get { return _inner.CurrentDeploymentName; }
        }

        string ICloudEnvironment.CurrentAssembliesName
        {
            get { return _inner.CurrentAssembliesName; }
        }

        void ICloudEnvironment.LoadDeployment(string deploymentName)
        {
            _inner.LoadDeployment(deploymentName);
        }

        void ICloudEnvironment.LoadCurrentHeadDeployment()
        {
            _inner.LoadCurrentHeadDeployment();
        }


        int ICloudEnvironment.CurrentWorkerInstanceCount
        {
            get { return _inner.CurrentWorkerInstanceCount; }
        }

        void ICloudEnvironment.ProvisionWorkerInstances(int numberOfInstances)
        {
            _inner.ProvisionWorkerInstances(numberOfInstances);
        }

        void ICloudEnvironment.ProvisionWorkerInstancesAtLeast(int minNumberOfInstances)
        {
            _inner.ProvisionWorkerInstancesAtLeast(minNumberOfInstances);
        }


        string ICloudEnvironment.GetSettingValue(string settingName)
        {
            return _inner.GetSettingValue(settingName);
        }

        X509Certificate2 ICloudEnvironment.GetCertificate(string thumbprint)
        {
            return _inner.GetCertificate(thumbprint);
        }

        string ICloudEnvironment.GetLocalResourcePath(string resourceName)
        {
            return _inner.GetLocalResourcePath(resourceName);
        }
    }
}
