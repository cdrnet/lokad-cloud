#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.Application;

namespace Lokad.Cloud.AssemblyLoading
{
    internal sealed class AssemblyLoader
    {
        public void LoadAssembliesIntoAppDomain(IEnumerable<AssemblyData> assembliesAndSymbols, ApplicationEnvironment environment)
        {
            var resolver = new AssemblyResolver();
            resolver.Attach();

            // Store files locally, because only pure IL assemblies can be loaded directly from memory
            var path = Path.Combine(
                environment.GetLocalResourcePath("AssembliesTemp"),
                Guid.NewGuid().ToString("N"));

            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }

            Directory.CreateDirectory(path);
            foreach (var assembly in assembliesAndSymbols)
            {
                File.WriteAllBytes(Path.Combine(path, assembly.Name), assembly.Bytes);
            }

            var assemblies = Directory.EnumerateFiles(path, "*.dll").Concat(Directory.EnumerateFiles(path, "*.exe"));
            foreach (var assembly in assemblies)
            {
                Assembly.LoadFile(assembly);
            }
        }
    }
}