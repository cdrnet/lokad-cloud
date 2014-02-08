#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using ICSharpCode.SharpZipLib.Zip;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.Services.AppContext
{
    [Serializable]
    public class DeploymentReader : IDeploymentReader
    {
        private const string ContainerName = "lokad-cloud-services-deployments";
        private const string HeadName = "HEAD.lokadcloud";
        private readonly string _connectionString;

        [NonSerialized] CloudStorageProviders _storage;

        public DeploymentReader(string storageConnectionString)
        {
            _connectionString = storageConnectionString;
        }

        private CloudStorageProviders Storage
        {
            get { return _storage ?? (_storage = CloudStorage.ForAzureConnectionString(_connectionString).BuildStorageProviders()); }
        }

        public XElement GetHeadIfModified(string knownETag, out string newETag)
        {
            return Storage.RawBlobStorage.GetBlobIfModified<XElement>(ContainerName, HeadName, knownETag, out newETag).GetValue((XElement)null);
        }

        public XElement GetDeployment(string deploymentName)
        {
            return Storage.RawBlobStorage.GetBlob<XElement>(ContainerName, deploymentName).GetValue(() => default(XElement));
        }

        public IEnumerable<Tuple<string, byte[]>> GetAssembliesAndSymbols(string assembliesName)
        {
            var archive = Storage.RawBlobStorage.GetBlob<byte[]>(ContainerName, assembliesName).GetValue(() => default(byte[]));
            if (archive == null)
                return null;

            var assemblyBytes = new List<Tuple<string, byte[]>>();

            var allowedExtensions = new HashSet<string> {".exe", ".dll", ".pdb"};

            using (var stream = new MemoryStream(archive))
            {
                using (var zipStream = new ZipInputStream(stream))
                {
                    ZipEntry entry;
                    while ((entry = zipStream.GetNextEntry()) != null)
                    {
                        if (!entry.IsFile || entry.Size == 0)
                        {
                            continue;
                        }

                        var extension = (Path.GetExtension(entry.Name) ?? "").ToLowerInvariant();
                        if (!allowedExtensions.Contains(extension))
                        {
                            continue;
                        }

                        var name = Path.GetFileNameWithoutExtension(entry.Name) ?? "";
                        var data = new byte[entry.Size];
                        try
                        {
                            zipStream.Read(data, 0, data.Length);
                        }
                        catch (Exception)
                        {
                            continue;
                        }

                        assemblyBytes.Add(Tuple.Create(entry.Name.ToLowerInvariant(), data));
                    }
                }
            }

            return assemblyBytes;
        }

        public T GetItem<T>(string itemName) where T : class
        {
            return Storage.RawBlobStorage.GetBlob<T>(ContainerName, itemName).GetValue(() => default(T));
        }
    }
}
