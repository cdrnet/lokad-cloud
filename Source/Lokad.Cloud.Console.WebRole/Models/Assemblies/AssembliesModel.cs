﻿#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.Services.Management.Application;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.Console.WebRole.Models.Assemblies
{
    public class AssembliesModel
    {
        public Maybe<CloudApplicationAssemblyInfo[]> ApplicationAssemblies { get; set; }
    }
}