#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.Services.Management.Deployments
{
    public class IndexManager
    {
        private const string ContainerName = "lokad-cloud-services-deployments";
        private const string DeploymentsIndexName = "INDEX.lokadcloud";

        private readonly CloudStorageProviders _storage;

        public IndexManager(CloudStorageProviders storage)
        {
            _storage = storage;
        }

        public void PublishDeployment(DeploymentReference deployment)
        {
            UpdateIndex(
                () => new XElement("Index",
                    new XElement("Deployments"),
                    new XElement("Assemblies")),
                xml =>
                    {
                        var deployments = xml.Element("Deployments");
                        if (deployments.Elements("Deployment").Any(d => d.Attribute("name").Value == deployment.Name))
                        {
                            // already in index
                            return false;
                        }

                        var now = DateTimeOffset.UtcNow;

                        var assemblies = xml.Element("Assemblies");
                        if (!assemblies.Elements("Assemblies").Any(d => d.Attribute("name").Value == deployment.AssembliesName))
                        {
                            assemblies.Add(new XElement("Assemblies",
                                new XAttribute("name", deployment.AssembliesName),
                                new XAttribute("firstSeen", now)));
                        }

                        deployments.Add(new XElement("Deployment",
                            new XAttribute("name", deployment.Name),
                            new XAttribute("firstSeen", now),
                            new XElement("Assemblies", new XAttribute("name", deployment.AssembliesName)),
                            new XElement("Config", new XAttribute("name", deployment.ConfigName)),
                            new XElement("Settings", new XAttribute("name", deployment.SettingsName))));

                        return true;
                    });
        }

        void UpdateIndex(Func<XElement> defaultEmpty, Func<XElement, bool> updateOrSkip)
        {
            _storage.RawBlobStorage.UpsertBlobOrSkip<byte[]>(ContainerName, DeploymentsIndexName,
                insert: () =>
                    {
                        var xml = defaultEmpty();

                        if (!updateOrSkip(xml))
                        {
                            return Maybe<byte[]>.Empty;
                        }

                        var outDocument = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), xml);
                        using (var stream = new MemoryStream())
                        {
                            outDocument.Save(stream);
                            return stream.ToArray();
                        }
                    },
                update: bytes =>
                    {
                        XElement xml;
                        using (var stream = new MemoryStream(bytes))
                        {
                            var inDocument = XDocument.Load(stream);
                            xml = inDocument.Root;
                        }

                        if (!updateOrSkip(xml))
                        {
                            return Maybe<byte[]>.Empty;
                        }

                        var outDocument = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), xml);
                        using (var stream = new MemoryStream())
                        {
                            outDocument.Save(stream);
                            return stream.ToArray();
                        }
                    });
        }
    }
}
