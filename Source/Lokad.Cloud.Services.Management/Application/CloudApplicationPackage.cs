﻿#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Collections.Generic;
using System.Reflection;

namespace Lokad.Cloud.Services.Management.Application
{
    public class CloudApplicationPackage
    {
        public List<CloudApplicationAssemblyInfo> Assemblies { get; set; }
        private readonly Dictionary<string, byte[]> _assemblyBytes;
        private readonly Dictionary<string, byte[]> _symbolBytes;

        public CloudApplicationPackage(List<CloudApplicationAssemblyInfo> assemblyInfos, Dictionary<string, byte[]> assemblyBytes, Dictionary<string, byte[]> symbolBytes)
        {
            Assemblies = assemblyInfos;
            _assemblyBytes = assemblyBytes;
            _symbolBytes = symbolBytes;
        }

        public byte[] GetAssembly(CloudApplicationAssemblyInfo assemblyInfo)
        {
            return _assemblyBytes[assemblyInfo.AssemblyName.ToLowerInvariant()];
        }

        public byte[] GetSymbol(CloudApplicationAssemblyInfo assemblyInfo)
        {
            return _symbolBytes[assemblyInfo.AssemblyName.ToLowerInvariant()];
        }

        public void LoadAssemblies()
        {
            var resolver = new AssemblyResolver();
            resolver.Attach();

            foreach (var info in Assemblies)
            {
                if (!info.IsValid)
                {
                    continue;
                }

                if (info.HasSymbols)
                {
                    Assembly.Load(GetAssembly(info), GetSymbol(info));
                }
                else
                {
                    Assembly.Load(GetAssembly(info));
                }
            }
        }
    }
}
