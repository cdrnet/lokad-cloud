#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.Management;
using Lokad.Cloud.Provisioning.Instrumentation;
using Lokad.Cloud.Provisioning.Instrumentation.Events;
using Lokad.Cloud.Storage.Azure;

namespace Lokad.Cloud
{
    /// <summary>
    /// IoC module that registers all usually required components, including
    /// storage providers, management & provisioning and diagnostics/logging.
    /// It is recommended to load this module even when only using the storage (O/C mapping) providers.
    /// Expects a <see cref="CloudConfigurationSettings"/> instance to be registered as well.
    /// </summary>
    /// <remarks>
    /// When only using the storage (O/C mapping) toolkit standalone it is easier
    /// to let the <see cref="Storage.CloudStorage"/> factory create the storage providers on demand.
    /// </remarks>
    /// <seealso cref="CloudConfigurationSettings"/>
    /// <seealso cref="CloudConfigurationModule"/>
    /// <seealso cref="Storage.CloudStorage"/>
    public sealed class CloudModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterModule(new StorageModule());
            builder.RegisterModule(new DiagnosticsModule());

            builder.RegisterType<Jobs.JobManager>();
            builder.RegisterType<ServiceFabric.RuntimeFinalizer>().As<IRuntimeFinalizer>().InstancePerLifetimeScope();

            // NOTE: Guid is not very nice, but this will be replaced anyway once fully ported to AppHost
            builder.Register(c =>
                new CloudEnvironment(
                    Guid.NewGuid().ToString("N"),
                    c.Resolve<CloudConfigurationSettings>(),
                    c.Resolve<ILog>(),
                    c.ResolveOptional<ICloudProvisioningObserver>()))
                .As<ICloudEnvironment, IProvisioningProvider>().SingleInstance();

            // Provisioning Observer Subject
            builder.Register(ProvisioningObserver)
                .As<ICloudProvisioningObserver, IObservable<ICloudProvisioningEvent>>()
                .SingleInstance();
        }

        static CloudProvisioningInstrumentationSubject ProvisioningObserver(IComponentContext c)
        {
            // will include any registered storage event observers, if there are any, as fixed subscriptions
            return new CloudProvisioningInstrumentationSubject(c.Resolve<IEnumerable<IObserver<ICloudProvisioningEvent>>>().ToArray());
        }
    }
}
