#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using System.Reactive.Linq;
using System.Xml.Linq;
using Lokad.Cloud.AppHost.Framework.Instrumentation;
using Lokad.Cloud.Provisioning.Instrumentation;
using Lokad.Cloud.Provisioning.Instrumentation.Events;
using Lokad.Cloud.Storage.Instrumentation;
using Lokad.Cloud.Storage.Instrumentation.Events;

namespace Lokad.Cloud.Diagnostics
{
    internal static class Observers
    {
        private static HostLogLevel EventLevelToLogLevel(int level)
        {
            switch (level)
            {
                case 1:
                    return HostLogLevel.Debug;
                case 2:
                    return HostLogLevel.Info;
                case 3:
                    return HostLogLevel.Warn;
                case 4:
                    return HostLogLevel.Error;
                case 5:
                    return HostLogLevel.Fatal;
                default:
                    throw new NotSupportedException(level.ToString());
            }
        }

        private static string ExtractExceptionText(XElement meta)
        {
            var exceptionXml = meta.Element("Exception");
            return exceptionXml != null && !String.IsNullOrWhiteSpace(exceptionXml.Value)
                ? exceptionXml.Value.Trim()
                : null;
        }

        public static IHostObserver CreateHostObserver(HostLogWriter log)
        {
            var subject = new HostObserverSubject();
            subject.Subscribe(e => TryLog(log, e.GetType(), (int)e.Level, e.Describe(), e.DescribeMeta()));
            return subject;
        }

        public static IProvisioningObserver CreateProvisioningObserver(HostLogWriter log)
        {
            var subject = new ProvisioningObserverSubject();
            subject.OfType<ProvisioningOperationRetriedEvent>()
                .Buffer(TimeSpan.FromMinutes(5))
                .Subscribe(events =>
                    {
                        foreach (var group in events.GroupBy(e => e.Policy))
                        {
                            var first = group.First();
                            TryLog(log, typeof(ProvisioningOperationRetriedEvent), (int)first.Level,
                                string.Format("Provisioning: {0} retries in 5 min for the {1} policy on {2}. {3}",
                                    group.Count(), group.Key, Environment.MachineName,
                                    string.Join(", ", group.Where(e => e.Exception != null).Select(e => e.Exception.GetType().Name).Distinct().ToArray())),
                                first.DescribeMeta());
                        }
                    });

            return subject;
        }

        public static IStorageObserver CreateStorageObserver(HostLogWriter log)
        {
            var subject = new StorageObserverSubject();
            subject.OfType<BlobDeserializationFailedEvent>().Subscribe(e => TryLog(log, HostLogLevel.Error, e.Exception, e.ToString()));
            subject.OfType<MessageDeserializationFailedQuarantinedEvent>().Subscribe(e => TryLog(log, HostLogLevel.Warn, e.Exceptions, e.ToString()));
            subject.OfType<MessageProcessingFailedQuarantinedEvent>().Subscribe(e => TryLog(log, HostLogLevel.Warn, e.ToString()));
            subject.OfType<MessagesRevivedEvent>().Subscribe(e => TryLog(log, HostLogLevel.Warn, e.ToString()));
            subject.OfType<StorageOperationRetriedEvent>()
                .Where(@event => @event.Policy != "OptimisticConcurrency")
                .ThrottleTokenBucket(TimeSpan.FromMinutes(15), 2)
                .Subscribe(@event =>
                {
                    var e = @event.Item;
                    TryLog(log, HostLogLevel.Debug, e.Exception, "Storage: Retried on policy {0}{1} on {2}.{3}",
                        e.Policy,
                        e.Exception != null ? " because of " + e.Exception.GetType().Name : string.Empty,
                        Environment.MachineName,
                        @event.DroppedItems > 0 ? string.Format(" There have been {0} similar events in the last 15 minutes.", @event.DroppedItems) : string.Empty);
                });

            return subject;
        }

        private static void TryLog(HostLogWriter log, Type eventType, int level, string message, XElement meta)
        {
            try
            {
                log.Log(EventLevelToLogLevel(level), ExtractExceptionText(meta), message, meta);
            }
            catch (Exception)
            {
                // If logging fails, ignore (we can't report)
            }
        }

        private static void TryLog(HostLogWriter log, HostLogLevel level, string message, params object[] args)
        {
            try
            {
                log.Log(level, args == null ? message : string.Format(message, args), null, null);
            }
            catch (Exception)
            {
                // If logging fails, ignore (we can't report)
            }
        }

        static void TryLog(HostLogWriter log, HostLogLevel level, Exception exception, string message, params object[] args)
        {
            try
            {
                log.Log(level, args == null ? message : string.Format(message, args), exception!= null ? exception.ToString() : null, null);
            }
            catch (Exception)
            {
                // If logging fails, ignore (we can't report)
            }
        }
    }
}
