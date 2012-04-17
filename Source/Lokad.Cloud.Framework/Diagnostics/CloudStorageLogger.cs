#region Copyright (c) Lokad 2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Lokad.Cloud.Storage.Instrumentation;
using Lokad.Cloud.Storage.Instrumentation.Events;

namespace Lokad.Cloud.Diagnostics
{
    // TODO (ruegg, 2011-05-30): Temporary class to maintain logging via system events for now -> rework

    internal class CloudStorageLogger : Autofac.IStartable, IDisposable
    {
        private readonly IEnvironment _environment;
        private readonly IObservable<IStorageEvent> _observable;
        private readonly ILog _log;
        private readonly List<IDisposable> _subscriptions;

        public CloudStorageLogger(IEnvironment environment, IObservable<IStorageEvent> observable, ILog log)
        {
            _environment = environment;
            _observable = observable;
            _log = log;
            _subscriptions = new List<IDisposable>();
        }

        void Autofac.IStartable.Start()
        {
            if (_log == null || _observable == null)
            {
                return;
            }

            _subscriptions.Add(_observable.OfType<BlobDeserializationFailedEvent>().Subscribe(e => _log.TryLog(LogLevel.Error, e.Describe(), e.Exception, e.DescribeMeta())));
            _subscriptions.Add(_observable.OfType<MessageDeserializationFailedQuarantinedEvent>().Subscribe(e => _log.TryLog(LogLevel.Warn, e.Describe(), e.Exceptions, e.DescribeMeta())));
            _subscriptions.Add(_observable.OfType<MessageProcessingFailedQuarantinedEvent>().Subscribe(e => _log.TryLog(LogLevel.Warn, e.Describe(), meta: e.DescribeMeta())));
            _subscriptions.Add(_observable.OfType<MessagesRevivedEvent>().Subscribe(e => _log.TryLog(LogLevel.Warn, e.Describe(), meta: e.DescribeMeta())));

            _subscriptions.Add(_observable.OfType<StorageOperationRetriedEvent>()
                .Where(@event => @event.Policy != "OptimisticConcurrency")
                .ThrottleTokenBucket(TimeSpan.FromMinutes(15), 2)
                .Subscribe(@event =>
                    {
                        var e = @event.Item;
                        _log.TryDebugFormat(e.Exception,"Storage: Retried on policy {0}{1} on {2}.{3}",
                            e.Policy,
                            e.Exception != null ? " because of " + e.Exception.GetType().Name : string.Empty,
                            _environment.Host.WorkerName,
                            @event.DroppedItems > 0 ? string.Format(" There have been {0} similar events in the last 15 minutes.", @event.DroppedItems) : string.Empty);
                    }));
        }

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
            {
                subscription.Dispose();
            }

            _subscriptions.Clear();
        }
    }
}
