﻿#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Text;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.Services.Management
{
    /// <summary>
    /// Management facade for cloud configuration.
    /// </summary>
    public class CloudConfiguration
    {
        const string AssembliesContainerName = "lokad-cloud-assemblies";
        const string ConfigurationBlobName = "config";

        readonly IBlobStorageProvider _blobs;
        readonly UTF8Encoding _encoding = new UTF8Encoding();

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudConfiguration"/> class.
        /// </summary>
        public CloudConfiguration(IBlobStorageProvider blobStorageProvider)
        {
            _blobs = blobStorageProvider;
        }

        /// <summary>
        /// Get the cloud configuration file.
        /// </summary>
        public string GetConfigurationString()
        {
            var buffer = _blobs.GetBlob<byte[]>(
                AssembliesContainerName,
                ConfigurationBlobName);

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
                AssembliesContainerName,
                ConfigurationBlobName,
                _encoding.GetBytes(configuration));
        }

        /// <summary>
        /// Remove the cloud configuration file.
        /// </summary>
        public void RemoveConfiguration()
        {
            _blobs.DeleteBlobIfExist(
                AssembliesContainerName,
                ConfigurationBlobName);
        }
    }
}