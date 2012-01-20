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
using Lokad.Cloud.Storage;

namespace Lokad.Cloud
{
    [Serializable]
    public class DeploymentReader : IDeploymentReader
    {
        private const string ContainerName = "lokad-cloud-assemblies";
        private const string PackageBlobName = "default";
        private const string AutofacConfigBlobName = "config";

        private readonly string _connectionString;
        private readonly string _subscriptionId;
        private readonly string _certificateThumbprint;

        public DeploymentReader(string connectionString, string subscriptionId, string certificateThumbprint)
        {
            _connectionString = connectionString;
            _subscriptionId = subscriptionId;
            _certificateThumbprint = certificateThumbprint;
            _storage = CloudStorage.ForAzureConnectionString(connectionString).BuildStorageProviders();
        }

        [NonSerialized]
        private CloudStorageProviders _storage;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            _storage = CloudStorage.ForAzureConnectionString(_connectionString).BuildStorageProviders();
        }

        public SolutionHead GetDeploymentIfModified(string knownETag, out string newETag)
        {
            newETag = CombineEtags(
                _storage.BlobStorage.GetBlobEtag(ContainerName, PackageBlobName),
                _storage.BlobStorage.GetBlobEtag(ContainerName, AutofacConfigBlobName));

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

            string appConfigEtag;
            var appConfig = _storage.BlobStorage.GetBlob<byte[]>(ContainerName, AutofacConfigBlobName, out appConfigEtag);
            if (appConfig.HasValue && appConfigEtag == AutofacConfigEtagOfCombinedEtag(deployment.SolutionId))
            {
                settings.Add(new XElement("AutofacAppConfig", Convert.ToBase64String(appConfig.Value)));
            }

            return new SolutionDefinition("Solution", new[]
                {
                    new CellDefinition("Cell",
                        new AssembliesHead(PackageEtagOfCombinedEtag(deployment.SolutionId)),
                        typeof(EntryPoint.ApplicationEntryPoint).FullName,
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

        static string CombineEtags(string packageEtag, string autofacConfigEtag)
        {
            if (packageEtag == null)
            {
                return null;
            }

            var prefix = packageEtag.Length.ToString("0000");
            return autofacConfigEtag == null
                ? string.Concat(prefix, packageEtag)
                : string.Concat(prefix, packageEtag, autofacConfigEtag);
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

        static string AutofacConfigEtagOfCombinedEtag(string combinedEtag)
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
