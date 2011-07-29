#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ICSharpCode.SharpZipLib.Zip;

namespace Lokad.Cloud.AppHost.AssembyLoading
{
    internal sealed class AssemblyLoader
    {
        public void LoadAssembliesIntoAppDomain(byte[] assemblyZipBytes)
        {
            // 1. UNPACK

            var assemblyBytes = new Dictionary<string, byte[]>();
            var symbolBytes = new Dictionary<string, byte[]>();

            using (var stream = new MemoryStream(assemblyZipBytes))
            {
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

                        var name = Path.GetFileNameWithoutExtension(entry.Name);
                        var data = new byte[entry.Size];
                        try
                        {
                            zipStream.Read(data, 0, data.Length);
                        }
                        catch (Exception)
                        {
                            continue;
                        }

                        switch (extension)
                        {
                            case ".dll":
                                assemblyBytes.Add(name.ToLowerInvariant(), data);
                                break;
                            case ".pdb":
                                symbolBytes.Add(name.ToLowerInvariant(), data);
                                break;
                        }
                    }
                }
            }

            // 2. LOAD

            var resolver = new AssemblyResolver();
            resolver.Attach();

            foreach (var assembly in assemblyBytes)
            {
                byte[] symbol;
                if (symbolBytes.TryGetValue(assembly.Key, out symbol))
                {
                    Assembly.Load(assembly.Value, symbol);
                }
                else
                {
                    Assembly.Load(assembly.Value);
                }
            }
        }
    }
}
