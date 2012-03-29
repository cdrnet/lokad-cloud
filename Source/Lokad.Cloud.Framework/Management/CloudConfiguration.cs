#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Text;
using Lokad.Cloud.ServiceFabric.Runtime;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.Management
{
    /// <summary>
    /// Management facade for cloud configuration.
    /// </summary>
    public class CloudConfiguration
    {
        readonly IBlobStorageProvider _blobs;
        readonly IDataSerializer _runtimeSerializer;
        readonly UTF8Encoding _encoding = new UTF8Encoding();

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudConfiguration"/> class.
        /// </summary>
        public CloudConfiguration(IBlobStorageProvider storage)
        {
            _blobs = storage;
            _runtimeSerializer = new CloudFormatter();
        }

        /// <summary>
        /// Get the cloud configuration file.
        /// </summary>
        public string GetConfigurationString()
        {
            var buffer = _blobs.GetBlob<byte[]>(
                AssemblyLoader.ContainerName,
                AssemblyLoader.ConfigurationBlobName,
                _runtimeSerializer);

            return buffer.Convert(bytes => _encoding.GetString(bytes), String.Empty);
        }

        /// <summary>
        /// Set or update the cloud configuration file.
        /// </summary>
        public void SetConfiguration(string configuration)
        {
            if(configuration == null)
            {
                RemoveConfiguration();
                return;
            }

            configuration = configuration.Trim();
            if(String.IsNullOrEmpty(configuration))
            {
                RemoveConfiguration();
                return;
            }

            _blobs.PutBlob(
                AssemblyLoader.ContainerName,
                AssemblyLoader.ConfigurationBlobName,
                _encoding.GetBytes(configuration),
                _runtimeSerializer);
        }

        /// <summary>
        /// Remove the cloud configuration file.
        /// </summary>
        public void RemoveConfiguration()
        {
            _blobs.DeleteBlobIfExist(
                AssemblyLoader.ContainerName,
                AssemblyLoader.ConfigurationBlobName);
        }
    }
}
