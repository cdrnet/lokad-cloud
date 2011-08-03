#region Copyright (c) Lokad 2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Lokad.Cloud.Services.Framework.Logging;
using Lokad.Cloud.Storage.Instrumentation.Events;

namespace Lokad.Cloud.Services.Framework.SystemServices
{
    /// <remarks>
    /// To change logging behavior, simply derive from this class, override the Subscribe method
    /// and disable this original service.
    /// </remarks>
    public class CloudStorageLoggingService : DaemonService
    {
        private readonly IObservable<ICloudStorageEvent> _observable;
        private readonly ILogWriter _log;
        private readonly List<IDisposable> _subscriptions;

        public CloudStorageLoggingService(IObservable<ICloudStorageEvent> observable, ILogWriter log)
        {
            _observable = observable;
            _log = log;
            _subscriptions = new List<IDisposable>();
        }

        public sealed override void OnStart()
        {
            if (_log == null || _observable == null)
            {
                return;
            }

            // Dispose old subscriptions, in case of bad protocol
            foreach(var subscription in _subscriptions)
            {
                subscription.Dispose();
            }
            _subscriptions.Clear();

            // Subscribe
            _subscriptions.AddRange(Subscribe(_observable));
        }

        public sealed override void OnStop()
        {
            foreach (var subscription in _subscriptions)
            {
                subscription.Dispose();
            }
            _subscriptions.Clear();
        }

        protected virtual IEnumerable<IDisposable> Subscribe(IObservable<ICloudStorageEvent> observable)
        {
            yield return observable.OfType<BlobDeserializationFailedEvent>().Subscribe(e => TryLog(e, e.Exception, LogLevel.Warn));
            yield return observable.OfType<MessageDeserializationFailedQuarantinedEvent>().Subscribe(e => TryLog(e, e.Exceptions, LogLevel.Warn));
            yield return observable.OfType<MessageProcessingFailedQuarantinedEvent>().Subscribe(e => TryLog(e, level: LogLevel.Warn));

            yield return observable
                .OfType<StorageOperationRetriedEvent>()
                .Buffer(TimeSpan.FromHours(1))
                .Subscribe(events =>
                {
                    foreach (var group in events.GroupBy(e => e.Policy))
                    {
                        TryLog(string.Format("Storage: {0} retries per hour for the {1} policy on {2}. {3}",
                            group.Count(), group.Key, CloudEnvironment.MachineName,
                            string.Join(", ", group.Where(e => e.Exception != null).Select(e => e.Exception.GetType().Name).Distinct().ToArray())),
                            level: LogLevel.Debug);
                    }
                });
        }

        protected void TryLog(object message, Exception exception = null, LogLevel level = LogLevel.Info)
        {
            try
            {
                if (exception != null)
                {
                    _log.Log(level, exception, message);
                }
                else
                {
                    _log.Log(level, message);
                }
            }
            catch (Exception)
            {
                // If logging fails, ignore (we can't report)
            }
        }
    }
}
