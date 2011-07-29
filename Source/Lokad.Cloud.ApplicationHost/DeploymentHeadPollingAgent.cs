#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.AppHost.Framework.Commands;
using Lokad.Cloud.AppHost.Util;

namespace Lokad.Cloud.AppHost
{
    internal class DeploymentHeadPollingAgent
    {
        private readonly IDeploymentReader _deploymentReader;

        private readonly Action<IHostCommand> _sendCommand;
        private string _knownHeadDeployment, _knownHeadEtag;

        public DeploymentHeadPollingAgent(IDeploymentReader deploymentReader, Action<IHostCommand> sendCommand)
        {
            _deploymentReader = deploymentReader;
            _sendCommand = sendCommand;
        }

        public void PollForChanges(string currentlyLoadedDeoplymentName = null)
        {
            string newEtag;
            var head = _deploymentReader.GetHeadIfModified(_knownHeadEtag, out newEtag);
            if (head == null)
            {
                if (_knownHeadDeployment != null && _knownHeadDeployment != currentlyLoadedDeoplymentName)
                {
                    // HEAD has not changed (or is missing), yet the provided current deployment
                    // doesn't match the HEAD as we know it and have last seen it -> LOAD
                    _sendCommand(new LoadDeploymentCommand(_knownHeadDeployment));
                }

                return;
            }

            var deploymentName = head.SettingsElementAttributeValue("Deployment", "name");

            _knownHeadEtag = newEtag;
            _knownHeadDeployment = deploymentName;

            if (currentlyLoadedDeoplymentName != deploymentName)
            {
                _sendCommand(new LoadDeploymentCommand(deploymentName));
            }
        }
    }
}
