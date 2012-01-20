#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Autofac.Configuration;
using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.Management;
using Lokad.Cloud.Mock;
using Lokad.Cloud.Provisioning.Instrumentation;
using Lokad.Cloud.Provisioning.Instrumentation.Events;
using Lokad.Cloud.ServiceFabric;
using Lokad.Cloud.Storage.Azure;

namespace Lokad.Cloud.Test
{
    public sealed class GlobalSetup
    {
        static IContainer _container;

        static IContainer Setup()
        {
            var builder = new ContainerBuilder();

            builder.RegisterModule(new StorageModule());
            builder.RegisterModule(new DiagnosticsModule());

            builder.RegisterType<Jobs.JobManager>();
            builder.RegisterType<RuntimeFinalizer>().As<IRuntimeFinalizer>().InstancePerLifetimeScope();

            builder.RegisterType<MockEnvironment>().As<ICloudEnvironment>();

            // Provisioning Observer Subject
            builder.Register(c => new CloudProvisioningInstrumentationSubject(c.Resolve<IEnumerable<IObserver<ICloudProvisioningEvent>>>().ToArray()))
                .As<ICloudProvisioningObserver, IObservable<ICloudProvisioningEvent>>()
                .SingleInstance();

            builder.RegisterModule(new ConfigurationSettingsReader("autofac"));

            return builder.Build();
        }

        /// <summary>Gets the IoC container as initialized by the setup.</summary>
        public static IContainer Container 
        { 
            get { return _container ?? (_container = Setup()); }
        }
    }
}