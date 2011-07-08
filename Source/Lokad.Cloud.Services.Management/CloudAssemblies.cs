#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using Lokad.Cloud.Services.Management.Application;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.Services.Management
{
    /// <summary>Management facade for cloud assemblies.</summary>
    public class CloudAssemblies
    {
        const string AssembliesContainerName = "lokad-cloud-assemblies";
        const string PackageBlobName = "default";

        readonly CloudStorageProviders _storage;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudAssemblies"/> class.
        /// </summary>
        public CloudAssemblies(CloudStorageProviders storage)
        {
            _storage = storage;
        }

        public Maybe<CloudApplicationDefinition> GetApplicationDefinition()
        {
            var inspector = new CloudApplicationInspector(_storage);
            return inspector.Inspect();
        }

        /// <summary>
        /// Configure a .dll assembly file as the new cloud service assembly.
        /// </summary>
        public void UploadApplicationSingleDll(byte[] data, string fileName)
        {
            using (var tempStream = new MemoryStream())
            {
                using (var zip = new ZipOutputStream(tempStream))
                {
                    zip.PutNextEntry(new ZipEntry(fileName));
                    zip.Write(data, 0, data.Length);
                    zip.CloseEntry();
                }

                UploadApplicationZipContainer(tempStream.ToArray());
            }
        }

        /// <summary>
        /// Configure a zip container with one or more assemblies as the new cloud services.
        /// </summary>
        public void UploadApplicationZipContainer(byte[] data)
        {
            _storage.NeutralBlobStorage.PutBlob(
                AssembliesContainerName,
                PackageBlobName,
                data,
                true);
        }

        /// <summary>
        /// Verify whether the provided zip container is valid.
        /// </summary>
        public bool IsValidZipContainer(byte[] data)
        {
            try
            {
                using (var dataStream = new MemoryStream(data))
                using (var zipStream = new ZipInputStream(dataStream))
                {
                    ZipEntry entry;
                    while ((entry = zipStream.GetNextEntry()) != null)
                    {
                        var buffer = new byte[entry.Size];
                        zipStream.Read(buffer, 0, buffer.Length);
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
