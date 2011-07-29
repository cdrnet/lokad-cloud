#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;
using System.Xml.Linq;
using Lokad.Cloud.Services.Framework.Commands;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.Services.Runtime.Agents
{
    internal class DeploymentHeadPollingAgent
    {
        private const string ContainerName = "lokad-cloud-services-deployments";
        private const string HeadName = "HEAD.lokadcloud";

        private readonly CloudStorageProviders _storage;
        private readonly Action<ICloudCommand> _sendCommand;
        private string _knownHeadDeployment, _knownHeadEtag;

        public DeploymentHeadPollingAgent(CloudStorageProviders storage, Action<ICloudCommand> sendCommand)
        {
            _storage = storage;
            _sendCommand = sendCommand;
        }

        public void PollForChanges(string currentlyLoadedDeoplymentName = null)
        {
            string newEtag;
            var headBytes = _storage.RawBlobStorage.GetBlobIfModified<byte[]>(ContainerName, HeadName, _knownHeadEtag, out newEtag);
            if (!headBytes.HasValue)
            {
                if (_knownHeadDeployment != null && _knownHeadDeployment != currentlyLoadedDeoplymentName)
                {
                    // HEAD has not changed (or is missing), yet the provided current deployment
                    // doesn't match the HEAD as we know it and have last seen it -> LOAD
                    _sendCommand(new RuntimeLoadDeploymentCommand(_knownHeadDeployment));
                }

                return;
            }

            string deploymentName;
            using (var stream = new MemoryStream(headBytes.Value))
            {
                var document = XDocument.Load(stream);
                deploymentName = document.Root.Element("Deployment").Attribute("name").Value;
            }

            _knownHeadEtag = newEtag;
            _knownHeadDeployment = deploymentName;

            if (currentlyLoadedDeoplymentName != deploymentName)
            {
                _sendCommand(new RuntimeLoadDeploymentCommand(deploymentName));
            }
        }
    }
}
