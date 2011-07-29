#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.IO;
using System.Text;
using System.Xml.Linq;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.Services.Management.Deployments
{
    public class DeploymentReader
    {
        private const string ContainerName = "lokad-cloud-services-deployments";

        private readonly CloudStorageProviders _storage;

        public DeploymentReader(CloudStorageProviders storage)
        {
            _storage = storage;
        }

        public Maybe<DeploymentReference> GetDeployment(string name)
        {
            var xml = GetXml(name);
            if (!xml.HasValue)
            {
                return Maybe<DeploymentReference>.Empty;
            }

            var root = xml.Value;
            return new DeploymentReference(name,
                root.Element("Assemblies").Attribute("name").Value,
                root.Element("Config").Attribute("name").Value,
                root.Element("Settings").Attribute("name").Value);
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
    }
}
