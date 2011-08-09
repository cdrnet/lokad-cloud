#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;
using System.Text;
using System.Xml.Linq;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.Services.Management.Build
{
    internal class DeploymentWriter
    {
        private const string ContainerName = "lokad-cloud-services-deployments";

        private readonly CloudStorageProviders _storage;
        private readonly IndexManager _index;
        private readonly ContentBasedNaming _naming;

        public DeploymentWriter(CloudStorageProviders storage)
        {
            _storage = storage;
            _index = new IndexManager(storage);
            _naming = new ContentBasedNaming();
        }

        /// <returns>The resulting name of the assemblies.</returns>
        public string WriteAssembliesZip(byte[] bytes)
        {
            return PutBytes(bytes, _naming.NameForAssemblies);
        }

        /// <returns>The resulting name of the config.</returns>
        public string WriteConfig(XElement xml)
        {
            return PutXml(xml, _naming.NameForConfig);
        }

        /// <returns>The resulting name of the config.</returns>
        public string WriteConfig(string text)
        {
            return PutString(text, _naming.NameForConfig);
        }

        /// <returns>The resulting name of the config.</returns>
        private string WriteConfig(byte[] bytes)
        {
            return PutBytes(bytes, _naming.NameForConfig);
        }

        /// <returns>The resulting name of the settings.</returns>
        public string WriteServices(XElement xml)
        {
            return PutXml(xml, _naming.NameForServices);
        }

        ///// <returns>The resulting name of the deployment.</returns>
        //public DeploymentReference WriteDeployment(string assembliesName, string configName, string settingsName)
        //{
        //    var name = PutXml(new XElement("Deployment",
        //        new XElement("Assemblies", new XAttribute("name", assembliesName)),
        //        new XElement("Config", new XAttribute("name", configName)),
        //        new XElement("Settings", new XAttribute("name", settingsName))),
        //        _naming.NameForDeployment);

        //    var reference = new DeploymentReference(name, assembliesName, configName, settingsName);

        //    _index.PublishDeployment(reference);

        //    return reference;
        //}

        string PutXml(XElement xmlRoot, Func<byte[], string> nameFor)
        {
            var document = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), xmlRoot);
            using (var stream = new MemoryStream())
            {
                document.Save(stream);
                return PutBytes(stream.ToArray(), nameFor);
            }
        }

        string PutString(string text, Func<byte[], string> nameFor)
        {
            return PutBytes(Encoding.UTF8.GetBytes(text), nameFor);
        }

        string PutBytes(byte[] data, Func<byte[], string> nameFor)
        {
            var name = nameFor(data);
            string etag;
            _storage.RawBlobStorage.PutBlob(ContainerName, name, data, typeof(byte[]), false, out etag);
            return name;
        }
    }
}
