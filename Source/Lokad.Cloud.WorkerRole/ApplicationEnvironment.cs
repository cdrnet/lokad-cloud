#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.AppHost.Framework.Commands;
using Lokad.Cloud.AppHost.Framework.Definition;

namespace Lokad.Cloud
{
    // TODO: temporary copy from AppHost (internal there), to be replaced with original once fully ported to AppHost

    /// <remarks>This class need to be able to cross AppDomains by reference, so all method arguments need to be serializable!</remarks>
    internal class ApplicationEnvironment : MarshalByRefObject, IApplicationEnvironment
    {
        private readonly IHostContext _hostContext;
        private readonly CellLifeIdentity _cellIdentity;
        private readonly SolutionHead _solution;
        private readonly AssembliesHead _assemblies;
        private readonly Action<IHostCommand> _sendCommand;

        internal ApplicationEnvironment(IHostContext hostContext, CellLifeIdentity cellIdentity, SolutionHead solution, AssembliesHead assemblies, Action<IHostCommand> sendCommand)
        {
            _hostContext = hostContext;
            _cellIdentity = cellIdentity;
            _solution = solution;
            _assemblies = assemblies;
            _sendCommand = sendCommand;
        }

        public HostLifeIdentity Host
        {
            get { return _hostContext.Identity; }
        }

        public CellLifeIdentity Cell
        {
            get { return _cellIdentity; }
        }

        public SolutionHead Deployment
        {
            get { return _solution; }
        }

        public AssembliesHead Assemblies
        {
            get { return _assemblies; }
        }

        public void LoadDeployment(SolutionHead deployment)
        {
            _sendCommand(new LoadDeploymentCommand(deployment));
        }

        public void LoadCurrentHeadDeployment()
        {
            _sendCommand(new LoadCurrentHeadDeploymentCommand());
        }

        public int CurrentWorkerInstanceCount
        {
            get { return _hostContext.CurrentWorkerInstanceCount; }
        }

        public void ProvisionWorkerInstances(int numberOfInstances)
        {
            _hostContext.ProvisionWorkerInstances(numberOfInstances);
        }

        public void ProvisionWorkerInstancesAtLeast(int minNumberOfInstances)
        {
            _hostContext.ProvisionWorkerInstancesAtLeast(minNumberOfInstances);
        }

        public string GetSettingValue(string settingName)
        {
            return _hostContext.GetSettingValue(settingName);
        }

        public X509Certificate2 GetCertificate(string thumbprint)
        {
            return _hostContext.GetCertificate(thumbprint);
        }

        public string GetLocalResourcePath(string resourceName)
        {
            return _hostContext.GetLocalResourcePath(resourceName);
        }

        public IPEndPoint GetEndpoint(string endpointName)
        {
            return _hostContext.GetEndpoint(endpointName);
        }

        public void SendCommand(IHostCommand command)
        {
            _sendCommand(command);
        }
    }
}
