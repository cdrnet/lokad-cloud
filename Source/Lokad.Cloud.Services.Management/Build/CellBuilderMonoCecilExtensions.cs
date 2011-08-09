#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;

namespace Lokad.Cloud.Services.Management.Build
{
    public static class CellBuilderMonoCecilExtensions
    {
        /// <summary>
        /// Include all the provided Mono.Cecil assemblies (dll, no symbols).
        /// </summary>
        /// <returns>The new assemblyName for the provided assemblies</returns>
        public static string Assemblies(this CellBuilder cellBuilder, IEnumerable<AssemblyDefinition> assemblies)
        {
            return cellBuilder.Assemblies(assemblies.Select(a => a.MainModule.FullyQualifiedName).Distinct().Select(p => new FileInfo(p)));
        }

        /// <summary>
        /// Include all the assemblies (dll, no symbols) where the provided Mono.Cecil types are defined in.
        /// </summary>
        /// <returns>The new assemblyName for the provided assemblies</returns>
        public static string Assemblies(this CellBuilder cellBuilder, IEnumerable<TypeDefinition> typesInAssemblies)
        {
            var paths = new HashSet<string>();

            foreach (var leafType in typesInAssemblies)
            {
                var type = leafType;
                while (type != null)
                {
                    paths.Add(type.Module.FullyQualifiedName);
                    if (type.BaseType == null)
                    {
                        break;
                    }

                    type = type.BaseType.Resolve();
                }
            }

            return cellBuilder.Assemblies(paths.Distinct().Select(p => new FileInfo(p)));
        }
    }
}
