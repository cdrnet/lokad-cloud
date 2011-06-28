﻿#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Lokad.Cloud.Runtime.Instrumentation;
using Lokad.Cloud.Runtime.Instrumentation.Events;
using Lokad.Cloud.Services.Framework.Logging;
using Lokad.Cloud.Storage;
using Microsoft.WindowsAzure;

namespace Lokad.Cloud.Diagnostics
{
    /// <summary>Cloud Diagnostics IoC Module.</summary>
    public class DiagnosticsModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(CloudLogWriter).As<ILogWriter>();

            // Runtime Observer Subject
            builder.Register(RuntimeObserver)
                .As<ICloudRuntimeObserver, IObservable<ICloudRuntimeEvent>>()
                .SingleInstance();

            // TODO (ruegg, 2011-05-30): Observer that logs system events to the log: temporary! to keep old logging behavior for now
            builder.RegisterType<CloudStorageLogger>().As<IStartable>().SingleInstance();
            builder.RegisterType<CloudProvisioningLogger>().As<IStartable>().SingleInstance();
        }

        static CloudLogWriter CloudLogWriter(IComponentContext c)
        {
            return new CloudLogWriter(BlobStorageForDiagnostics(c));
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

        static CloudRuntimeInstrumentationSubject RuntimeObserver(IComponentContext c)
        {
            // will include any registered storage event observers, if there are any, as fixed subscriptions
            return new CloudRuntimeInstrumentationSubject(c.Resolve<IEnumerable<IObserver<ICloudRuntimeEvent>>>().ToArray());
        }
    }
}
