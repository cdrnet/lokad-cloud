#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Xml.Linq;
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

        public T GetItem<T>(string itemName) where T : class
        {
            return Storage.RawBlobStorage.GetBlob<T>(ContainerName, itemName).GetValue(() => default(T));
        }
    }
}
