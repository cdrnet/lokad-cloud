#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Autofac;
using Lokad.Cloud.Framework.Autofac.Diagnostics;
using Lokad.Cloud.Storage;
using Lokad.Cloud.Storage.Autofac;

namespace Lokad.Cloud.Framework.Autofac
{
    /// <summary>
    /// IoC Module for using Lokad.Cloud with in-memory storage
    /// 
    /// Expected external registrations:
    /// - Lokad.Cloud.AppHost.Framework.IApplicationEnvironment
    /// </summary>
    public class MemoryModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterModule<MemoryStorageModule>();
            builder.RegisterModule<DiagnosticsModule>();

            builder.RegisterType<CloudFormatter>().As<IDataSerializer>();
            builder.RegisterType<Jobs.JobManager>();
        }
    }
}
