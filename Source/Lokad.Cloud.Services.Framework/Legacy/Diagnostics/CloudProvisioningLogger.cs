﻿#region Copyright (c) Lokad 2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Lokad.Cloud.Provisioning.Instrumentation.Events;
using Lokad.Cloud.Services.Framework.Logging;

namespace Lokad.Cloud.Diagnostics
{
    // TODO (ruegg, 2011-05-30): Temporary class to maintain logging via system events for now -> rework

    internal class CloudProvisioningLogger : Autofac.IStartable, IDisposable
    {
        private readonly IObservable<ICloudProvisioningEvent> _observable;
        private readonly ILogWriter _log;
        private readonly List<IDisposable> _subscriptions;

        public CloudProvisioningLogger(IObservable<ICloudProvisioningEvent> observable, ILogWriter log)
        {
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

            _subscriptions.Add(_observable.OfType<ProvisioningOperationRetriedEvent>()
                .Buffer(TimeSpan.FromMinutes(5))
                .Subscribe(events =>
                    {
                        foreach (var group in events.GroupBy(e => e.Policy))
                        {
                            TryLog(string.Format("Provisioning: {0} retries in 5 min for the {1} policy on {2}. {3}",
                                group.Count(), group.Key, CloudEnvironment.PartitionKey,
                                string.Join(", ", group.Where(e => e.Exception != null).Select(e => e.Exception.GetType().Name).Distinct().ToArray())),
                                level: LogLevel.Debug);
                        }
                    }));
        }

        void TryLog(object message, Exception exception = null, LogLevel level = LogLevel.Warn)
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
