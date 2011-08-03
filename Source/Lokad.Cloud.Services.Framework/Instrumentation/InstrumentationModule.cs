#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Lokad.Cloud.Storage.Instrumentation;
using Lokad.Cloud.Storage.Instrumentation.Events;

namespace Lokad.Cloud.Services.Framework.Instrumentation
{
    /// <summary>Cloud Diagnostics IoC Module.</summary>
    public class InstrumentationModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(StorageObserver)
                .As<ICloudStorageObserver, IObservable<ICloudStorageEvent>>()
                .SingleInstance();
        }

        static CloudStorageInstrumentationSubject StorageObserver(IComponentContext c)
        {
            // will include any registered storage event observers, if there are any, as fixed subscriptions
            return new CloudStorageInstrumentationSubject(c.Resolve<IEnumerable<IObserver<ICloudStorageEvent>>>().ToArray());
        }
    }
}
