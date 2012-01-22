#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.Autofac;
using Lokad.Cloud.Mock;
using Lokad.Cloud.Provisioning.Instrumentation;
using Lokad.Cloud.Provisioning.Instrumentation.Events;
using Lokad.Cloud.ServiceFabric;
using Microsoft.WindowsAzure;

namespace Lokad.Cloud.Test
{
    public sealed class GlobalSetup
    {
        static IContainer _container;

        static IContainer Setup()
        {
            var builder = new ContainerBuilder();

            builder.RegisterModule(new StorageModule(CloudStorageAccount.DevelopmentStorageAccount));
            builder.RegisterModule(new DiagnosticsModule());

            builder.RegisterType<RuntimeFinalizer>().As<IRuntimeFinalizer>().InstancePerLifetimeScope();
            builder.RegisterType<MockEnvironment>().As<IApplicationEnvironment>().InstancePerLifetimeScope();

            builder.RegisterType<Jobs.JobManager>();

            // Provisioning Observer Subject
            builder.Register(c => new CloudProvisioningInstrumentationSubject(c.Resolve<IEnumerable<IObserver<ICloudProvisioningEvent>>>().ToArray()))
                .As<ICloudProvisioningObserver, IObservable<ICloudProvisioningEvent>>()
                .SingleInstance();

            return builder.Build();
        }

        /// <summary>Gets the IoC container as initialized by the setup.</summary>
        public static IContainer Container 
        { 
            get { return _container ?? (_container = Setup()); }
        }
    }
}