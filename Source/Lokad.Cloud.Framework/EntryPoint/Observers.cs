#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using System.Reactive.Linq;
using Lokad.Cloud.AppHost.Framework.Instrumentation;
using Lokad.Cloud.AppHost.Framework.Instrumentation.Events;
using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.Instrumentation;
using Lokad.Cloud.Provisioning.Instrumentation;
using Lokad.Cloud.Provisioning.Instrumentation.Events;
using Lokad.Cloud.Storage.Instrumentation;
using Lokad.Cloud.Storage.Instrumentation.Events;

namespace Lokad.Cloud.EntryPoint
{
    internal static class Observers
    {
        public static IHostObserver CreateHostObserver(ILog log)
        {
            var subject = new HostObserverSubject();
            subject.OfType<HostStartedEvent>().Subscribe(e => TryLog(log, LogLevel.Debug, "AppHost started on {0}.", e.Host.WorkerName));
            subject.OfType<HostStoppedEvent>().Subscribe(e => TryLog(log, LogLevel.Debug, "AppHost stopped on {0}.", e.Host.WorkerName));
            subject.OfType<NewDeploymentDetectedEvent>().Subscribe(e => TryLog(log, LogLevel.Info, "New deployment {0} detected for solution {1} on {2}.", e.Deployment.SolutionId, e.Solution.SolutionName, e.Host.WorkerName));
            subject.OfType<NewUnrelatedSolutionDetectedEvent>().Subscribe(e => TryLog(log, LogLevel.Info, "New unrelated solution {0} detected on {1}.", e.Solution.SolutionName, e.Host.WorkerName));
            subject.OfType<CellStartedEvent>().Subscribe(e => TryLog(log, LogLevel.Debug, "Cell {0} of solution {1} started on {2}.", e.Cell.CellName, e.Cell.SolutionName, e.Cell.Host.WorkerName));
            subject.OfType<CellStoppedEvent>().Subscribe(e => TryLog(log, LogLevel.Debug, "Cell {0} of solution {1} stopped on {2}.", e.Cell.CellName, e.Cell.SolutionName, e.Cell.Host.WorkerName));
            subject.OfType<CellExceptionRestartedEvent>().Subscribe(e => TryLog(log, LogLevel.Error, "Cell {0} of solution {1} exception: {2}", e.Cell.CellName, e.Cell.SolutionName, e.Exception));
            subject.OfType<CellFatalErrorRestartedEvent>().Subscribe(e => TryLog(log, LogLevel.Fatal, "Cell {0} of solution {1} fatal error: {2}", e.Cell.CellName, e.Cell.SolutionName, e.Exception));

            return subject;
        }

        public static IProvisioningObserver CreateProvisioningObserver(ILog log)
        {
            var subject = new ProvisioningObserverSubject();
            subject.OfType<ProvisioningOperationRetriedEvent>()
                .Buffer(TimeSpan.FromMinutes(5))
                .Subscribe(events =>
                    {
                        foreach (var group in events.GroupBy(e => e.Policy))
                        {
                            TryLog(log, LogLevel.Debug, "Provisioning: {0} retries in 5 min for the {1} policy on {2}. {3}",
                                group.Count(), group.Key, Environment.MachineName,
                                string.Join(", ", group.Where(e => e.Exception != null).Select(e => e.Exception.GetType().Name).Distinct().ToArray()));
                        }
                    });

            return subject;
        }

        public static ICloudStorageObserver CreateStorageObserver(ILog log)
        {
            var subject = new CloudStorageInstrumentationSubject();
            subject.OfType<BlobDeserializationFailedEvent>().Subscribe(e => TryLog(log, LogLevel.Error, e.Exception, e.ToString()));
            subject.OfType<MessageDeserializationFailedQuarantinedEvent>().Subscribe(e => TryLog(log, LogLevel.Warn, e.Exceptions, e.ToString()));
            subject.OfType<MessageProcessingFailedQuarantinedEvent>().Subscribe(e => TryLog(log, LogLevel.Warn, e.ToString()));
            subject.OfType<MessagesRevivedEvent>().Subscribe(e => TryLog(log, LogLevel.Warn, e.ToString()));
            subject.OfType<StorageOperationRetriedEvent>()
                .Where(@event => @event.Policy != "OptimisticConcurrency")
                .ThrottleTokenBucket(TimeSpan.FromMinutes(15), 2)
                .Subscribe(@event =>
                {
                    var e = @event.Item;
                    TryLog(log, LogLevel.Debug, e.Exception, "Storage: Retried on policy {0}{1} on {2}.{3}",
                        e.Policy,
                        e.Exception != null ? " because of " + e.Exception.GetType().Name : string.Empty,
                        Environment.MachineName,
                        @event.DroppedItems > 0 ? string.Format(" There have been {0} similar events in the last 15 minutes.", @event.DroppedItems) : string.Empty);
                });

            return subject;
        }

        public static ICloudRuntimeObserver CreateRuntimeObserver(ILog log)
        {
            var subject = new CloudRuntimeInstrumentationSubject();

            return subject;
        }

        private static void TryLog(ILog log, LogLevel level, string message, params object[] args)
        {
            try
            {
                log.Log(level, args == null ? message : string.Format(message, args));
            }
            catch (Exception)
            {
                // If logging fails, ignore (we can't report)
            }
        }

        static void TryLog(ILog log, LogLevel level, Exception exception, string message, params object[] args)
        {
            try
            {
                log.Log(level, exception, args == null ? message : string.Format(message, args));
            }
            catch (Exception)
            {
                // If logging fails, ignore (we can't report)
            }
        }
    }
}
