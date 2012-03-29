#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using Ionic.Zip;
using Mono.Cecil;

namespace Lokad.Cloud.Application
{
    public class CloudApplicationPackageReader
    {
        public CloudApplicationPackage ReadPackage(byte[] data, bool fetchVersion)
        {
            using(var stream = new MemoryStream(data))
            {
                return ReadPackage(stream, fetchVersion);
            }
        }

        public CloudApplicationPackage ReadPackage(Stream stream, bool fetchVersion)
        {
            var assemblyInfos = new List<CloudApplicationAssemblyInfo>();
            var assemblyBytes = new Dictionary<string, byte[]>();
            var symbolBytes = new Dictionary<string, byte[]>();

            using (var zip = ZipFile.Read(stream))
            foreach (var entry in zip)
            {
                if (entry.IsDirectory || entry.IsText || entry.UncompressedSize == 0)
                {
                    continue;
                }

                var extension = Path.GetExtension(entry.FileName).ToLowerInvariant();
                if (extension != ".dll" && extension != ".pdb")
                {
                    continue;
                }

                var isValid = true;
                var name = Path.GetFileNameWithoutExtension(entry.FileName);
                using (var fileStream = new MemoryStream())
                {
                    try
                    {
                        entry.Extract(fileStream);
                    }
                    catch (Exception)
                    {
                        isValid = false;
                    }

                    switch (extension)
                    {
                        case ".dll":
                            assemblyBytes.Add(name.ToLowerInvariant(), fileStream.ToArray());
                            assemblyInfos.Add(new CloudApplicationAssemblyInfo
                                {
                                    AssemblyName = name,
                                    DateTime = entry.LastModified,
                                    SizeBytes = entry.UncompressedSize,
                                    IsValid = isValid,
                                    Version = new Version()
                                });
                            break;
                        case ".pdb":
                            symbolBytes.Add(name.ToLowerInvariant(), fileStream.ToArray());
                            break;
                    }
                }
            }

            foreach (var assemblyInfo in assemblyInfos)
            {
                assemblyInfo.HasSymbols = symbolBytes.ContainsKey(assemblyInfo.AssemblyName.ToLowerInvariant());
            }

            if (fetchVersion)
            {
                foreach (var assemblyInfo in assemblyInfos)
                {
                    byte[] bytes = assemblyBytes[assemblyInfo.AssemblyName.ToLowerInvariant()];

                    try
                    {
                        using (var assemblyStream = new MemoryStream(bytes))
                        {
                            var definition = AssemblyDefinition.ReadAssembly(assemblyStream);
                            assemblyInfo.Version = definition.Name.Version;
                        }
                    }
                    catch (Exception)
                    {
                        assemblyInfo.IsValid = false;
                    }
                }
            }

            return new CloudApplicationPackage(assemblyInfos, assemblyBytes, symbolBytes);
        }
    }
}
