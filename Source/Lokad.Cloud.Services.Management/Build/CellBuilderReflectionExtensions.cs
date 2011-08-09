#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Lokad.Cloud.Services.Management.Build
{
    public static class CellBuilderReflectionExtensions
    {
        /// <summary>
        /// Include all the provided System.Reflection assemblies (dll, no symbols).
        /// </summary>
        /// <returns>The new assemblyName for the provided assemblies</returns>
        public static string Assemblies(this CellBuilder cellBuilder, IEnumerable<Assembly> assemblies)
        {
            return cellBuilder.Assemblies(assemblies.Select(a => a.Location).Distinct().Select(p => new FileInfo(p)));
        }

        /// <summary>
        /// Include all the assemblies (dll, no symbols) where the provided types are defined in.
        /// </summary>
        /// <returns>The new assemblyName for the provided assemblies</returns>
        public static string Assemblies(this CellBuilder cellBuilder, IEnumerable<Type> typesInAssemblies)
        {
            var paths = new HashSet<string>();
            var referenceType = typeof(object);
            var valueType = typeof(ValueType);

            foreach (var leafType in typesInAssemblies)
            {
                var type = leafType;
                while (type != null && type != referenceType && type != valueType)
                {
                    paths.Add(type.Assembly.Location);
                    type = type.BaseType;
                }
            }

            return cellBuilder.Assemblies(paths.Distinct().Select(p => new FileInfo(p)));
        }
    }
}
