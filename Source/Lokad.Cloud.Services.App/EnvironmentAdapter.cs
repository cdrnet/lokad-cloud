#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.Services.Framework;

namespace Lokad.Cloud.Services.App
{
    internal class EnvironmentAdapter : ICloudEnvironment
    {
        private readonly IApplicationEnvironment _inner;

        public EnvironmentAdapter(IApplicationEnvironment inner)
        {
            _inner = inner;
        }

        string ICloudEnvironment.RuntimeMachineName
        {
            get { return _inner.MachineName; }
        }

        string ICloudEnvironment.RuntimeCellName
        {
            get { return _inner.CellName; }
        }

        string ICloudEnvironment.ApplicationDeploymentName
        {
            get { return _inner.CurrentDeploymentName; }
        }

        int ICloudEnvironment.WorkerInstanceCount
        {
            get { return _inner.CurrentWorkerInstanceCount; }
        }

        void ICloudEnvironment.LoadDeployment(string deploymentName)
        {
            _inner.LoadDeployment(deploymentName);
        }

        void ICloudEnvironment.LoadCurrentHeadDeployment()
        {
            _inner.LoadCurrentHeadDeployment();
        }

        void ICloudEnvironment.ProvisionWorkerInstances(int numberOfInstances)
        {
            _inner.ProvisionWorkerInstances(numberOfInstances);
        }

        void ICloudEnvironment.ProvisionWorkerInstancesAtLeast(int minNumberOfInstances)
        {
            _inner.ProvisionWorkerInstancesAtLeast(minNumberOfInstances);
        }
    }
}
