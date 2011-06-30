﻿#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Autofac;
using Lokad.Cloud.ServiceFabric;
using Lokad.Cloud.Services.Framework.Instrumentation;
using Lokad.Cloud.Services.Framework.Jobs;
using Lokad.Cloud.Services.Framework.Logging;
using Lokad.Cloud.Services.Framework.Storage;

namespace Lokad.Cloud.Services.Framework
{
    /// <summary>
    /// IoC module that registers all usually required components, including
    /// storage providers, management & provisioning and diagnostics/logging.
    /// It is recommended to load this module even when only using the storage (O/C mapping) providers.
    /// Expects the <see cref="CloudConfigurationModule"/> (or the mock module) to be registered as well.
    /// </summary>
    /// <remarks>
    /// When only using the storage (O/C mapping) toolkit standalone it is easier
    /// to let the <see cref="Standalone"/> factory create the storage providers on demand.
    /// </remarks>
    /// <seealso cref="CloudConfigurationModule"/>
    /// <seealso cref="Standalone"/>
    public sealed class CloudModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterModule(new StorageModule());
            builder.RegisterModule(new InstrumentationModule());
            builder.RegisterModule(new LoggingModule());
            //builder.RegisterModule(new Management.ManagementModule());

            builder.RegisterType<JobManager>();
            builder.RegisterType<RuntimeFinalizer>().As<IRuntimeFinalizer>().InstancePerLifetimeScope();
        }
    }
}
