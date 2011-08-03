#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Autofac;
using Lokad.Cloud.Services.Management.Logging;

namespace Lokad.Cloud.Services.Management
{
    /// <summary>
    /// IoC module for Lokad.Cloud management classes.
    /// </summary>
    public class ManagementModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<CloudLogReader>();
        }
    }
}
