#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Reactive.Linq;
using Lokad.Cloud.AppHost.Framework.Instrumentation;
using Lokad.Cloud.Provisioning.Instrumentation;
using Lokad.Cloud.Provisioning.Instrumentation.Events;
using Lokad.Cloud.Storage.Instrumentation;
using Lokad.Cloud.Storage.Instrumentation.Events;

namespace Lokad.Cloud.Diagnostics
{
    internal static class Observers
    {
        public static IHostObserver CreateHostObserver(HostLogWriter log)
        {
            var subject = new HostObserverSubject();
            subject.Subscribe(e => log.TryLog((HostLogLevel)(int)e.Level, e.Describe(), meta: e.DescribeMeta()));

            return subject;
        }

        public static IProvisioningObserver CreateProvisioningObserver(HostLogWriter log)
        {
            var subject = new ProvisioningObserverSubject();

            subject.Where(e => !(e is ProvisioningOperationRetriedEvent))
                .Subscribe(e => log.TryLog((HostLogLevel)(int)e.Level, e.Describe(), meta: e.DescribeMeta()));

            subject.OfType<ProvisioningOperationRetriedEvent>()
                .ThrottleTokenBucket(TimeSpan.FromMinutes(15), 2)
                .Subscribe(@event =>
                {
                    var e = @event.Item;
                    log.TryLog(HostLogLevel.Debug, string.Format("Provisioning: Retried on policy {0}{1} on {2}.{3}",
                        e.Policy,
                        e.Exception != null ? " because of " + e.Exception.GetType().Name : string.Empty,
                        Environment.MachineName,
                        @event.DroppedItems > 0 ? string.Format(" There have been {0} similar events in the last 15 minutes.", @event.DroppedItems) : string.Empty),
                        e.Exception, e.DescribeMeta());
                });

            return subject;
        }

        public static IStorageObserver CreateStorageObserver(HostLogWriter log)
        {
            var subject = new StorageObserverSubject();

            subject.Where(e => !(e is StorageOperationRetriedEvent) && !(e is StorageOperationSucceededEvent))
                .Subscribe(e => log.TryLog((HostLogLevel)(int)e.Level, e.Describe(), meta: e.DescribeMeta()));

            subject.OfType<StorageOperationRetriedEvent>()
                .Where(@event => @event.Policy != "OptimisticConcurrency")
                .ThrottleTokenBucket(TimeSpan.FromMinutes(15), 2)
                .Subscribe(@event =>
                {
                    var e = @event.Item;
                    log.TryLog(HostLogLevel.Debug, string.Format("Storage: Retried on policy {0}{1} on {2}.{3}",
                        e.Policy,
                        e.Exception != null ? " because of " + e.Exception.GetType().Name : string.Empty,
                        Environment.MachineName,
                        @event.DroppedItems > 0 ? string.Format(" There have been {0} similar events in the last 15 minutes.", @event.DroppedItems) : string.Empty),
                        e.Exception, e.DescribeMeta());
                });

            return subject;
        }
    }
}
