#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Reactive.Linq;
using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.Instrumentation;
using Lokad.Cloud.Storage.Instrumentation;
using Lokad.Cloud.Storage.Instrumentation.Events;

namespace Lokad.Cloud.Services.EntryPoint.Diagnostics
{
    public static class SimpleLoggingObservers
    {
        public static StorageObserverSubject CreateForStorage(ILog log, IObserver<IStorageEvent>[] fixedObservers = null)
        {
            var subject = new StorageObserverSubject(fixedObservers);

            subject.Where(e => !(e is StorageOperationRetriedEvent) && !(e is StorageOperationSucceededEvent))
                .Subscribe(e => log.TryLog((LogLevel)(int)e.Level, e.Describe(), meta: e.DescribeMeta()));

            subject.OfType<StorageOperationRetriedEvent>()
                .Where(@event => @event.Policy != "OptimisticConcurrency")
                .ThrottleTokenBucket(TimeSpan.FromMinutes(15), 2)
                .Subscribe(@event =>
                {
                    var e = @event.Item;
                    log.TryLog(LogLevel.Debug, string.Format("Storage: Retried on policy {0}{1} on {2}.{3}",
                        e.Policy,
                        e.Exception != null ? " because of " + e.Exception.GetType().Name : string.Empty,
                        Environment.MachineName,
                        @event.DroppedItems > 0 ? string.Format(" There have been {0} similar events in the last 15 minutes.", @event.DroppedItems) : string.Empty),
                        e.Exception, e.DescribeMeta());
                });

            return subject;
        }

        public static RuntimeObserverSubject CreateForRuntime(ILog log, IObserver<IRuntimeEvent>[] fixedObservers = null)
        {
            var subject = new RuntimeObserverSubject(fixedObservers);
            subject.Subscribe(e => log.TryLog((LogLevel)(int)e.Level, e.Describe(), meta: e.DescribeMeta()));

            return subject;
        }

        public static ApplicationObserverSubject CreateForApplication(ILog log, IObserver<IApplicationEvent>[] fixedObservers = null)
        {
            var subject = new ApplicationObserverSubject(fixedObservers);
            subject.Subscribe(e => log.TryLog((LogLevel)(int)e.Level, e.Describe(), meta: e.DescribeMeta()));

            return subject;
        }
    }
}
