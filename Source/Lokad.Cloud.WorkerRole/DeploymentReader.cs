#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Xml.Linq;
using Ionic.Zip;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.AppHost.Framework.Definition;
using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud
{
    [Serializable]
    public class DeploymentReader : IDeploymentReader
    {
        private const string ContainerName = "lokad-cloud-assemblies";
        private const string PackageBlobName = "default";
        private const string ConfigBlobName = "config";

        private readonly string _connectionString;
        private readonly string _subscriptionId;
        private readonly string _certificateThumbprint;

        public DeploymentReader(string connectionString, string subscriptionId, string certificateThumbprint)
        {
            _connectionString = connectionString;
            _subscriptionId = subscriptionId;
            _certificateThumbprint = certificateThumbprint;

            _log = new HostLogWriter(CloudStorage.ForAzureConnectionString(connectionString).BuildBlobStorage());
            _storage = CloudStorage.ForAzureConnectionString(connectionString).WithObserver(LoggingObservers.CreateForStorage(_log)).BuildStorageProviders();
        }

        [NonSerialized]
        private CloudStorageProviders _storage;

        [NonSerialized]
        private HostLogWriter _log;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            _log = new HostLogWriter(CloudStorage.ForAzureConnectionString(_connectionString).BuildBlobStorage());
            _storage = CloudStorage.ForAzureConnectionString(_connectionString).WithObserver(LoggingObservers.CreateForStorage(_log)).BuildStorageProviders();
        }

        public SolutionHead GetDeploymentIfModified(string knownETag, out string newETag)
        {
            newETag = CombineEtags(
                _storage.BlobStorage.GetBlobEtag(ContainerName, PackageBlobName),
                _storage.BlobStorage.GetBlobEtag(ContainerName, ConfigBlobName));

            if (newETag == null || knownETag != null && knownETag == newETag)
            {
                return null;
            }

            return new SolutionHead(newETag);
        }

        public SolutionDefinition GetSolution(SolutionHead deployment)
        {
            var settings = new XElement("Settings",
                    new XElement("DataConnectionString", _connectionString),
                    new XElement("CertificateThumbprint", _certificateThumbprint),
                    new XElement("SubscriptionId", _subscriptionId));

            var entryPointTypeName = "Lokad.Cloud.Framework.Autofac.AutofacApplicationEntryPoint, Lokad.Cloud.Framework.Autofac";
            var solutionName = "Lokad.Cloud";

            string configEtag;
            var appConfig = _storage.BlobStorage.GetBlob<byte[]>(ContainerName, ConfigBlobName, out configEtag);
            if (appConfig.HasValue && configEtag == ConfigEtagOfCombinedEtag(deployment.SolutionId))
            {
                // add raw config to settings (Base64)
                settings.Add(new XElement("RawConfig", Convert.ToBase64String(appConfig.Value)));

                // directly insert config xml root as element, if possible
                try
                {
                    using (var configStream = new MemoryStream(appConfig.Value))
                    {
                        var configDoc = XDocument.Load(configStream);
                        if (configDoc != null && configDoc.Root != null)
                        {
                            settings.Add(configDoc.Root);

                            // if root contains "EntryPoint" element with "typeName" attribute, use it as entry point
                            var entryPointXml = configDoc.Root.Element("EntryPoint");
                            XAttribute typeNameXml;
                            if (entryPointXml != null && (typeNameXml = entryPointXml.Attribute("typeName")) != null && !String.IsNullOrWhiteSpace(typeNameXml.Value))
                            {
                                entryPointTypeName = typeNameXml.Value.Trim();
                            }

                            // if root contains "Solution" element, use its value as solution name
                            var solutionXml = configDoc.Root.Element("Solution");
                            if (solutionXml != null && !String.IsNullOrWhiteSpace(solutionXml.Value))
                            {
                                solutionName = solutionXml.Value.Trim();
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // don't care, unfortunately there's no TryLoad
                }
            }

            return new SolutionDefinition(solutionName, new[]
                {
                    new CellDefinition("Primary",
                        new AssembliesHead(PackageEtagOfCombinedEtag(deployment.SolutionId)),
                        entryPointTypeName,
                        settings.ToString())
                });
        }

        public IEnumerable<AssemblyData> GetAssembliesAndSymbols(AssembliesHead assemblies)
        {
            string packageEtag;
            var packageBlob = _storage.BlobStorage.GetBlob<byte[]>(ContainerName, PackageBlobName, out packageEtag);
            if (!packageBlob.HasValue || packageEtag != assemblies.AssembliesId)
            {
                yield break;
            }

            using (var zipStream = new MemoryStream(packageBlob.Value))
            using (var zip = ZipFile.Read(zipStream))
            {
                foreach (var entry in zip)
                {
                    if (entry.IsDirectory || entry.IsText || entry.UncompressedSize == 0)
                    {
                        continue;
                    }

                    var extension = Path.GetExtension(entry.FileName);
                    if (extension != ".dll" && extension != ".pdb")
                    {
                        continue;
                    }

                    using (var stream = new MemoryStream())
                    {
                        entry.Extract(stream);
                        yield return new AssemblyData(Path.GetFileName(entry.FileName), stream.ToArray());
                    }
                }
            }
        }

        public T GetItem<T>(string itemName) where T : class
        {
            return _storage.BlobStorage.GetBlob<T>(ContainerName, itemName).GetValue(default(T));
        }

        static string CombineEtags(string packageEtag, string configEtag)
        {
            if (packageEtag == null)
            {
                return null;
            }

            var prefix = packageEtag.Length.ToString("0000");
            return configEtag == null
                ? string.Concat(prefix, packageEtag)
                : string.Concat(prefix, packageEtag, configEtag);
        }

        static string PackageEtagOfCombinedEtag(string combinedEtag)
        {
            if (combinedEtag == null || combinedEtag.Length <= 4)
            {
                return null;
            }

            var packageEtag = combinedEtag.Substring(4, Int32.Parse(combinedEtag.Substring(0, 4)));
            return string.IsNullOrEmpty(packageEtag) ? null : packageEtag;
        }

        static string ConfigEtagOfCombinedEtag(string combinedEtag)
        {
            if (combinedEtag == null || combinedEtag.Length <= 5)
            {
                return null;
            }

            var configEtag = combinedEtag.Substring(4 + Int32.Parse(combinedEtag.Substring(0, 4)));
            return string.IsNullOrEmpty(configEtag) ? null : configEtag;
        }
    }
}
