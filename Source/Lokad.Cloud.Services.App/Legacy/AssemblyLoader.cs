#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.Services.Management.Application;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.Services.Runtime.Legacy
{
    /// <remarks>
    /// Since the assemblies are loaded in the current <c>AppDomain</c>, this class
    /// should be a natural candidate for a singleton design pattern. Yet, keeping
    /// it as a plain class facilitates the IoC instantiation.
    /// </remarks>
    internal class AssemblyLoader
    {
        const string AssembliesContainerName = "lokad-cloud-assemblies";
        const string PackageBlobName = "default";
        const string ConfigurationBlobName = "config";

        readonly IBlobStorageProvider _blobs;

        /// <summary>Build a new package loader.</summary>
        public AssemblyLoader(IBlobStorageProvider blobStorageProvider)
        {
            _blobs = blobStorageProvider;
        }

        /// <summary>Loads the assembly package.</summary>
        /// <remarks>This method is expected to be called only once.</remarks>
        public void LoadPackage()
        {
            var buffer = _blobs.GetBlob<byte[]>(AssembliesContainerName, PackageBlobName);

            // if no assemblies have been loaded yet, just skip the loading
            if (!buffer.HasValue)
            {
                return;
            }

            var reader = new CloudApplicationPackageReader();
            var package = reader.ReadPackage(buffer.Value, false);

            package.LoadAssemblies();
        }

        public Maybe<byte[]> LoadConfiguration()
        {
            return _blobs.GetBlob<byte[]>(AssembliesContainerName, ConfigurationBlobName);
        }
    }
}