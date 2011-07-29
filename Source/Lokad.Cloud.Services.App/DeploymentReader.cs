#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;
using System.Xml.Linq;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.Services.App
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
            return Storage.RawBlobStorage.GetBlobIfModified<byte[]>(ContainerName, HeadName, knownETag, out newETag)
                .Convert(bytes =>
                    {
                        using (var stream = new MemoryStream(bytes))
                        {
                            return XDocument.Load(stream).Root;
                        }
                    }, (XElement)null);
        }

        public T GetItem<T>(string itemName) where T : class
        {
            var bytes = Storage.RawBlobStorage.GetBlob<byte[]>(ContainerName, itemName);
            if (!bytes.HasValue)
            {
                return default(T);
            }

            var type = typeof(T);
            if (type.IsAssignableFrom(typeof(byte[])))
            {
                return bytes.Value as T;
            }

            if (type.IsAssignableFrom(typeof(XElement)))
            {
                using (var stream = new MemoryStream(bytes.Value))
                {
                    return XDocument.Load(stream).Root as T;
                }
            }

            throw new NotSupportedException();
        }
    }
}
