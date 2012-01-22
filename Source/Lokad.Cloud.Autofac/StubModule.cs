#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Autofac;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.Autofac.Storage;
using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.ServiceFabric;
using Lokad.Cloud.Storage;
using Lokad.Cloud.Stubs;

namespace Lokad.Cloud.Autofac
{
    /// <summary>
    /// IoC Module for using Lokad.Cloud as stub only, e.g. for unit testing
    /// </summary>
    public class StubModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterModule<StubStorageModule>();
            builder.RegisterInstance(StubLog.Instance).As<ILog>();

            builder.RegisterType<CloudFormatter>().As<IDataSerializer>();
            builder.RegisterType<RuntimeFinalizer>().As<IRuntimeFinalizer>().SingleInstance();
            builder.RegisterType<Jobs.JobManager>();

            builder.RegisterType<StubEnvironment>().As<IApplicationEnvironment>();
        }
    }
}
