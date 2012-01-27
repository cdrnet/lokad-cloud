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
using Lokad.Cloud.Storage.Autofac;
using Lokad.Cloud.Storage.Instrumentation;

namespace Lokad.Cloud.Framework.Autofac.Diagnostics
{
    /// <summary>
    /// IoC Module for diagnostics and logging. Assumes that one of the storage modules has been registered as well.
    /// 
    /// Registers storage, runtime and application observers with default logging.
    /// You can of course override these registrations if you don't want them.
    /// </summary>
    public class DiagnosticsModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(c => new CloudLogWriter(c.Resolve<NeutralLogStorage>().BlobStorage)).As<ILog>();

            // Storage Observer Subject
            builder.Register(c => SimpleLoggingObservers.CreateForStorage(c.Resolve<ILog>(), c.ResolveOptional<IEnumerable<IObserver<IStorageEvent>>>().ToArray()))
                .As<IStorageObserver, IObservable<IStorageEvent>>()
                .SingleInstance();

            // Runtime Observer Subject
            builder.Register(c => SimpleLoggingObservers.CreateForRuntime(c.Resolve<ILog>(), c.ResolveOptional<IEnumerable<IObserver<IRuntimeEvent>>>().ToArray()))
                .As<IRuntimeObserver, IObservable<IRuntimeEvent>>()
                .SingleInstance();

            // Application Observer Subject
            builder.Register(c => SimpleLoggingObservers.CreateForApplication(c.Resolve<ILog>(), c.ResolveOptional<IEnumerable<IObserver<IApplicationEvent>>>().ToArray()))
                .As<IApplicationObserver, IObservable<IApplicationEvent>>()
                .SingleInstance();
        }
    }
}
