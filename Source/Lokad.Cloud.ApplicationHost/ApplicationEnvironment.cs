#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.AppHost.Framework.Commands;

namespace Lokad.Cloud.AppHost
{
    /// <remarks>This class need to be able to cross AppDomains by reference, so all method arguments need to be serializable!</remarks>
    internal class ApplicationEnvironment : MarshalByRefObject, IApplicationEnvironment
    {
        private readonly IHostContext _hostContext;
        private readonly HostHandle _hostHandle;
        private readonly CellHandle _cellHandle;

        internal ApplicationEnvironment(IHostContext hostContext, HostHandle hostHandle, CellHandle cellHandle)
        {
            _hostContext = hostContext;
            _hostHandle = hostHandle;
            _cellHandle = cellHandle;
        }

        public string MachineName
        {
            get { return _hostHandle.MachineName.Value; }
        }

        public string CellName
        {
            get { return _cellHandle.CellName; }
        }

        public string CurrentDeploymentName
        {
            get { return _cellHandle.CurrentDeploymentName; }
        }

        public string CurrentAssembliesName
        {
            get { return _cellHandle.CurretAssembliesName; }
        }

        public int CurrentWorkerInstanceCount
        {
            get { return _hostContext.CurrentWorkerInstanceCount; }
        }

        public void LoadDeployment(string deploymentName)
        {
            _hostHandle.SendCommand(new LoadDeploymentCommand(deploymentName));
        }

        public void LoadCurrentHeadDeployment()
        {
            _hostHandle.SendCommand(new LoadCurrentHeadDeploymentCommand());
        }

        public void ProvisionWorkerInstances(int numberOfInstances)
        {
            throw new NotImplementedException();
        }

        public void ProvisionWorkerInstancesAtLeast(int minNumberOfInstances)
        {
            throw new NotImplementedException();
        }

        public void SendCommand(IHostCommand command)
        {
            _hostHandle.SendCommand(command);
        }
    }
}
