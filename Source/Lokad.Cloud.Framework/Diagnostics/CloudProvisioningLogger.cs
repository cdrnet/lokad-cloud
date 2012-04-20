#region Copyright (c) Lokad 2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Lokad.Cloud.Provisioning.Instrumentation;
using Lokad.Cloud.Provisioning.Instrumentation.Events;

namespace Lokad.Cloud.Diagnostics
{
    // TODO (ruegg, 2011-05-30): Temporary class to maintain logging via system events for now -> rework

    internal class CloudProvisioningLogger : Autofac.IStartable, IDisposable
    {
        private readonly IEnvironment _environment;
        private readonly IObservable<IProvisioningEvent> _observable;
        private readonly ILog _log;
        private readonly List<IDisposable> _subscriptions;

        public CloudProvisioningLogger(IEnvironment environment, IObservable<IProvisioningEvent> observable, ILog log)
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

            _subscriptions.Add(_observable
                .OfType<ProvisioningOperationRetriedEvent>()
                .Buffer(TimeSpan.FromMinutes(5))
                .Subscribe(events =>
                    {
                        foreach (var group in events.GroupBy(e => e.Policy))
                        {
                            _log.TryDebugFormat("Provisioning: {0} retries in 5 min for the {1} policy on {2}. {3}",
                                group.Count(), group.Key, _environment.Host.WorkerName,
                                string.Join(", ", group.Where(e => e.Exception != null).Select(e => e.Exception.GetType().Name).Distinct().ToArray()));
                        }
                    }));

            _subscriptions.Add(_observable
                .OfType<ProvisioningUpdateInstanceCountEvent>()
                .Subscribe(@event => _log.Info().WithMeta(@event.DescribeMeta())
                    .TryWriteFormat("Provisioning: requesting {0} workers, from currently {1}", @event.RequestedInstanceCount, @event.CurrentInstanceCount)));

            _subscriptions.Add(_observable
                .Where(e => !(e is ProvisioningOperationRetriedEvent) && !(e is ProvisioningUpdateInstanceCountEvent))
                .Subscribe(@event => _log.TryDebug(@event.Describe(), meta: @event.DescribeMeta())));
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
