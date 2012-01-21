#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.IO;
using System.Linq;
using Ionic.Zip;
using Lokad.Cloud.Management.Application;
using Lokad.Cloud.Runtime;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.Management
{
    /// <summary>Management facade for cloud assemblies.</summary>
    public class CloudAssemblies
    {
        // <summary>Name of the container used to store the assembly package.</summary>
        public const string ContainerName = "lokad-cloud-assemblies";

        /// <summary>Name of the blob used to store the assembly package.</summary>
        public const string PackageBlobName = "default";

        readonly RuntimeProviders _runtimeProviders;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudAssemblies"/> class.
        /// </summary>
        public CloudAssemblies(RuntimeProviders runtimeProviders)
        {
            _runtimeProviders = runtimeProviders;
        }

        public Maybe<CloudApplicationDefinition> GetApplicationDefinition()
        {
            var inspector = new CloudApplicationInspector(_runtimeProviders);
            return inspector.Inspect();
        }

        /// <summary>
        /// Configure a .dll assembly file as the new cloud service assembly.
        /// </summary>
        public void UploadApplicationSingleDll(byte[] data, string fileName)
        {
            using (var stream = new MemoryStream())
            using (var zip = new ZipFile())
            {
                zip.AddEntry(fileName, data);
                zip.Save(stream);
                UploadApplicationZipContainer(stream.ToArray());
            }
        }

        /// <summary>
        /// Configure a zip container with one or more assemblies as the new cloud services.
        /// </summary>
        public void UploadApplicationZipContainer(byte[] data)
        {
            _runtimeProviders.BlobStorage.PutBlob(ContainerName, PackageBlobName, data, true);
        }

        /// <summary>
        /// Verify whether the provided zip container is valid.
        /// </summary>
        public bool IsValidZipContainer(byte[] data)
        {
            try
            {
                using (var dataStream = new MemoryStream(data))
                using (var zip = ZipFile.Read(dataStream))
                using (var readStream = new MemoryStream())
                foreach (var entry in zip.Where(e => !e.IsDirectory && !e.IsText && e.CompressedSize > 0))
                {
                    readStream.Position = 0;
                    entry.Extract(readStream);
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
