﻿#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using Mono.Cecil;

namespace Lokad.Cloud.Services.Management.Application
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

            using (var zipStream = new ZipInputStream(stream))
            {
                ZipEntry entry;
                while ((entry = zipStream.GetNextEntry()) != null)
                {
                    if (!entry.IsFile || entry.Size == 0)
                    {
                        continue;
                    }

                    var extension = Path.GetExtension(entry.Name).ToLowerInvariant();
                    if (extension != ".dll" && extension != ".pdb")
                    {
                        continue;
                    }

                    var isValid = true;
                    var name = Path.GetFileNameWithoutExtension(entry.Name);
                    var data = new byte[entry.Size];
                    try
                    {
                        zipStream.Read(data, 0, data.Length);
                    }
                    catch (Exception)
                    {
                        isValid = false;
                    }

                    switch (extension)
                    {
                        case ".dll":
                            assemblyBytes.Add(name.ToLowerInvariant(), data);
                            assemblyInfos.Add(new CloudApplicationAssemblyInfo
                                {
                                    AssemblyName = name,
                                    DateTime = entry.DateTime,
                                    SizeBytes = entry.Size,
                                    IsValid = isValid,
                                    Version = new Version()
                                });
                            break;
                        case ".pdb":
                            symbolBytes.Add(name.ToLowerInvariant(), data);
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
