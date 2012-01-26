#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Autofac;
using Lokad.Cloud.Autofac.Diagnostics;
using Lokad.Cloud.ServiceFabric;
using Lokad.Cloud.Storage;
using Lokad.Cloud.Storage.Autofac;

namespace Lokad.Cloud.Autofac
{
    /// <summary>
    /// IoC Module for using Lokad.Cloud with Azure storage.
    /// 
    /// Expected external registrations:
    /// - Microsoft.WindowsAzure.CloudStorageAccount
    /// - Lokad.Cloud.AppHost.Framework.IApplicationEnvironment
    /// </summary>
    public class AzureModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterModule<AzureStorageModule>();
            builder.RegisterModule<DiagnosticsModule>();

            builder.RegisterType<CloudFormatter>().As<IDataSerializer>();
            builder.RegisterType<RuntimeFinalizer>().As<IRuntimeFinalizer>().SingleInstance();
            builder.RegisterType<Jobs.JobManager>();
        }
    }
}
