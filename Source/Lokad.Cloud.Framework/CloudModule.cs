#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Autofac;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud
{
    /// <summary>
    /// IoC module that registers all usually required components, including
    /// storage providers, management and provisioning and diagnostics/logging.
    /// It is recommended to load this module even when only using the storage (O/C mapping) providers.
    /// Expects the <see cref="CloudConfigurationModule"/> (or the mock module) to be registered as well.
    /// </summary>
    /// <remarks>
    /// When only using the storage (O/C mapping) toolkit standalone it is easier
    /// to let the <see cref="CloudStorage"/> factory create the storage providers on demand.
    /// </remarks>
    /// <seealso cref="CloudConfigurationModule"/>
    /// <seealso cref="CloudStorage"/>
    public sealed class CloudModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<EnvironmentAdapter>().As<IEnvironment>();

            builder.RegisterModule(new Storage.Azure.StorageModule());
            builder.RegisterModule(new Diagnostics.DiagnosticsModule());
            builder.RegisterModule(new Management.ManagementModule());

            builder.RegisterType<Jobs.JobManager>();
        }
    }
}
