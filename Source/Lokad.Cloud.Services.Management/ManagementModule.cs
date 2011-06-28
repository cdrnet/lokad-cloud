#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Lokad.Cloud.Management;
using Lokad.Cloud.Provisioning.Instrumentation;
using Lokad.Cloud.Provisioning.Instrumentation.Events;
using Lokad.Cloud.Services.Framework.Logging;
using Lokad.Cloud.Services.Management.Logging;
using Lokad.Cloud.Storage;

using Microsoft.WindowsAzure;

namespace Lokad.Cloud.Services.Management
{
    /// <summary>
    /// IoC module for Lokad.Cloud management classes.
    /// </summary>
    public class ManagementModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // in some cases (like standalone mock storage) the RoleConfigurationSettings
            // will not be available. That's ok, since in this case Provisioning is not
            // available anyway and there's no need to make Provisioning resolveable.
            builder.Register(c => new CloudProvisioning(
                    c.Resolve<ICloudConfigurationSettings>(), 
                    c.Resolve<ILogWriter>()))
                .As<CloudProvisioning, IProvisioningProvider>()
                .SingleInstance();

            builder.Register(CloudLogReader);

            // Provisioning Observer Subject
            builder.Register(ProvisioningObserver)
                .As<ICloudProvisioningObserver, IObservable<ICloudProvisioningEvent>>()
                .SingleInstance();
        }

        static CloudLogReader CloudLogReader(IComponentContext c)
        {
            return new CloudLogReader(BlobStorageForDiagnostics(c));
        }

        static IBlobStorageProvider BlobStorageForDiagnostics(IComponentContext c)
        {
            // Neither log nor observers are provided since the providers
            // used for logging obviously can't log themselves (cyclic dependency)

            // We also always use the CloudFormatter, so this is equivalent
            // to the RuntimeProvider, for the same reasons.

            return CloudStorage
                .ForAzureAccount(c.Resolve<CloudStorageAccount>())
                .WithDataSerializer(new CloudFormatter())
                .BuildBlobStorage();
        }

        static CloudProvisioningInstrumentationSubject ProvisioningObserver(IComponentContext c)
        {
            // will include any registered storage event observers, if there are any, as fixed subscriptions
            return new CloudProvisioningInstrumentationSubject(c.Resolve<IEnumerable<IObserver<ICloudProvisioningEvent>>>().ToArray());
        }
    }
}
