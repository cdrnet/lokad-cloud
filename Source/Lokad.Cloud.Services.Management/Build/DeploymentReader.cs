#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.Services.Management.Build
{
    internal class DeploymentReader
    {
        private const string ContainerName = "lokad-cloud-services-deployments";

        private readonly CloudStorageProviders _storage;

        public DeploymentReader(CloudStorageProviders storage)
        {
            _storage = storage;
        }

        public Maybe<byte[]> GetBytes(string name)
        {
            return _storage.RawBlobStorage.GetBlob<byte[]>(ContainerName, name);
        }

        public Maybe<string> GetString(string name)
        {
            var bytes = GetBytes(name);
            return bytes.HasValue ? Encoding.UTF8.GetString(bytes.Value) : Maybe<string>.Empty;
        }

        public Maybe<XElement> GetXml(string name)
        {
            var bytes = GetBytes(name);
            if (!bytes.HasValue)
            {
                return Maybe<XElement>.Empty;
            }

            using (var stream = new MemoryStream(bytes.Value))
            {
                var document = XDocument.Load(stream);
                return document.Root;
            }
        }

        public XElement ReadDeploymentCellXml(string deploymentName, string cellName)
        {
            var root = ReadDeploymentXml(deploymentName);
            var cells = root.Element("Cells");
            if (cells == null)
            {
                throw new ArgumentException(string.Format("Cell with the name {0} not found in the deployment named {1}.", cellName, deploymentName), "cellName");
            }

            var cell = cells.Elements("Cell").FirstOrDefault(x => x.AttributeValue("name") == cellName);
            if (cell == null)
            {
                throw new ArgumentException(string.Format("Cell with the name {0} not found in the deployment named {1}.", cellName, deploymentName), "cellName");
            }

            return cell;
        }

        public XElement ReadDeploymentXml(string deploymentName)
        {
            var xml = GetXml(deploymentName);
            if (!xml.HasValue)
            {
                throw new ArgumentException(string.Format("Deployment with the name {0} not found.", deploymentName), "deploymentName");
            }

            var deployment = xml.Value;
            if (deployment == null || deployment.Name != "Deployment")
            {
                throw new ArgumentException(string.Format("Deployment with the name {0} is invalid.", deploymentName), "deploymentName");
            }

            return deployment;
        }
    }
}
