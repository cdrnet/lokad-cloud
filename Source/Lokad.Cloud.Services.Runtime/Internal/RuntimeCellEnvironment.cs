#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using Lokad.Cloud.Services.Framework.Commands;
using Lokad.Cloud.Services.Framework.Runtime;

namespace Lokad.Cloud.Services.Runtime.Internal
{
    /// <remarks>This class need to be able to cross AppDomains by reference, so all method arguments need to be serializable!</remarks>
    internal class RuntimeCellEnvironment : MarshalByRefObject, ICloudEnvironment
    {
        private readonly RuntimeHandle _runtime;
        private readonly CellHandle _cell;

        internal RuntimeCellEnvironment(RuntimeHandle runtime, CellHandle cell)
        {
            _runtime = runtime;
            _cell = cell;
        }

        public string RuntimeMachineName
        {
            get { return _runtime.MachineName.Value; }
        }

        public string RuntimeCellName
        {
            get { return _cell.CellName; }
        }

        public string ApplicationDeploymentName
        {
            get { return _runtime.ApplicationDeploymentName; }
        }

        public string AzureDeploymentId
        {
            get { return _runtime.AzureDeploymentId; }
        }

        public int WorkerInstanceCount
        {
            get { return _runtime.WorkerInstanceCount; }
        }

        public void LoadDeployment(string deploymentName)
        {
            _runtime.SendCommand(new RuntimeLoadDeploymentCommand(deploymentName));
        }

        public void LoadCurrentHeadDeployment()
        {
            _runtime.SendCommand(new RuntimeLoadCurrentHeadDeploymentCommand());
        }

        public void ProvisionWorkerInstances(int numberOfInstances)
        {
            throw new NotImplementedException();
        }

        public void ProvisionWorkerInstancesAtLeast(int minNumberOfInstances)
        {
            throw new NotImplementedException();
        }

        public void SendCommand(ICloudCommand command)
        {
            _runtime.SendCommand(command);
        }
    }
}
