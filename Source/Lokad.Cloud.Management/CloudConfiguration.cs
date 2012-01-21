#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Text;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.Management
{
    /// <summary>
    /// Management facade for cloud configuration.
    /// </summary>
    public class CloudConfiguration
    {
        /// <summary>Name of the container used to store the assembly package.</summary>
        public const string ContainerName = "lokad-cloud-assemblies";

        /// <summary>Name of the blob used to store the optional dependency injection configuration.</summary>
        public const string ConfigurationBlobName = "config";

        private readonly IBlobStorageProvider _blobs;
        private readonly IDataSerializer _runtimeFormatter;
        private readonly UTF8Encoding _encoding = new UTF8Encoding();

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudConfiguration"/> class.
        /// </summary>
        public CloudConfiguration(IBlobStorageProvider blobStorage)
        {
            _blobs = blobStorage;
            _runtimeFormatter = new CloudFormatter();
        }

        /// <summary>
        /// Get the cloud configuration file.
        /// </summary>
        public string GetConfigurationString()
        {
            var buffer = _blobs.GetBlob<byte[]>(ContainerName, ConfigurationBlobName, _runtimeFormatter);
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

            _blobs.PutBlob(ContainerName, ConfigurationBlobName, _encoding.GetBytes(configuration), _runtimeFormatter);
        }

        /// <summary>
        /// Remove the cloud configuration file.
        /// </summary>
        public void RemoveConfiguration()
        {
            _blobs.DeleteBlobIfExist(ContainerName, ConfigurationBlobName);
        }
    }
}
