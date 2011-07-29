#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.Services.Framework.Commands;

namespace Lokad.Cloud.Services.Framework.Runtime
{
    public interface ICloudEnvironment
    {
        string RuntimeMachineName { get; }
        string RuntimeCellName { get; }
        string ApplicationDeploymentName { get; }

        int WorkerInstanceCount { get; }

        void LoadDeployment(string deploymentName);
        void LoadCurrentHeadDeployment();

        void ProvisionWorkerInstances(int numberOfInstances);
        void ProvisionWorkerInstancesAtLeast(int minNumberOfInstances);

        void SendCommand(ICloudCommand command);
    }
}
