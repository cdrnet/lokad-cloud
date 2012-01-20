#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
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
        private const string ConfigBlobName = "config";

        private readonly string _storageConnectionString;
        
        public DeploymentReader(string storageConnectionString)
        {
            _storageConnectionString = storageConnectionString;
            _storage = CloudStorage.ForAzureConnectionString(storageConnectionString).BuildStorageProviders();
        }

        [NonSerialized]
        private CloudStorageProviders _storage;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            _storage = CloudStorage.ForAzureConnectionString(_storageConnectionString).BuildStorageProviders();
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
            return new SolutionDefinition("Solution", new[]
                {
                    new CellDefinition("Cell",
                        new AssembliesHead(PackageEtagOfCombinedEtag(deployment.SolutionId)),
                        typeof(EntryPoint.ApplicationEntryPoint).FullName,
                        null)
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

            return combinedEtag.Substring(4, Int32.Parse(combinedEtag.Substring(0, 4)));
        }
    }
}
