#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.Instrumentation;
using Lokad.Cloud.Instrumentation.Events;
using Lokad.Cloud.Provisioning.Instrumentation;
using Lokad.Cloud.Storage.Instrumentation;

namespace Lokad.Cloud.Autofac.Diagnostics
{
    /// <summary>
    /// IoC Module for diagnostics and logging. Assumes that one of the storage modules has been registered as well.
    /// </summary>
    public class DiagnosticsModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(c => new CloudLogWriter(c.Resolve<NeutralLogStorage>().BlobStorage)).As<ILog>();

            // Storage Observer Subject
            builder.Register(c => new StorageObserverSubject(c.ResolveOptional<IEnumerable<IObserver<IStorageEvent>>>().ToArray()))
                .As<IStorageObserver, IObservable<IStorageEvent>>()
                .SingleInstance();

            // Runtime Observer Subject
            builder.Register(c => new CloudRuntimeInstrumentationSubject(c.ResolveOptional<IEnumerable<IObserver<ICloudRuntimeEvent>>>().ToArray()))
                .As<ICloudRuntimeObserver, IObservable<ICloudRuntimeEvent>>()
                .SingleInstance();

            // Provisioning Observer Subject
            builder.Register(c => new ProvisioningObserverSubject(c.ResolveOptional<IEnumerable<IObserver<IProvisioningEvent>>>().ToArray()))
                .As<IProvisioningObserver, IObservable<IProvisioningEvent>>()
                .SingleInstance();

            // TODO (ruegg, 2011-05-30): Observer that logs system events to the log: temporary! to keep old logging behavior for now
            builder.RegisterType<CloudStorageLogger>().As<IStartable>().SingleInstance();
            builder.RegisterType<CloudProvisioningLogger>().As<IStartable>().SingleInstance();
        }
    }
}
