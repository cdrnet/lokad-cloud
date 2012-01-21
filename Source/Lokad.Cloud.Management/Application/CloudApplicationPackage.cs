#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Collections.Generic;

namespace Lokad.Cloud.Management.Application
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
    }
}
