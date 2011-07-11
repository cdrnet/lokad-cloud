#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Lokad.Cloud.Services.Framework;
using Lokad.Cloud.Services.Management.Settings;

namespace Lokad.Cloud.Services.Runtime.Runner
{
    internal class ScheduledWorkerServiceRunner : CommonServiceRunner
    {
        private readonly SortedSet<DateTimeOffset> _triggers;
        private readonly List<ScheduledWorkerServiceContext> _services;

        public ScheduledWorkerServiceRunner(List<ServiceWithSettings<ScheduledWorkerService, ScheduledWorkerServiceSettings>> services)
            : base(services.Select(s => s.Service))
        {
            var now = DateTimeOffset.UtcNow;
            _services = services.Select(s => new ScheduledWorkerServiceContext(s.Service, s.Settings, now)).ToList();
            _triggers = new SortedSet<DateTimeOffset>(_services.Select(c => c.NextRun));
        }

        public bool RunSingle(CancellationToken cancellationToken)
        {
            if (_triggers.Count == 0 || _triggers.Min > DateTimeOffset.UtcNow || cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            var trigger = _triggers.Min;
            var services = _services.Where(c => c.NextRun == trigger).ToList();

            if (services.Count >= 1)
            {
                var service = services[0];

                service.NextRun = trigger + service.Settings.TriggerInterval;
                _triggers.Add(service.NextRun);
                if (services.Count == 1)
                {
                    _triggers.Remove(trigger);
                }

                service.Service.OnSchedule(trigger, cancellationToken);
                return true;
            }

            _triggers.Remove(trigger);
            return false;
        }

        class ScheduledWorkerServiceContext
        {
            public ScheduledWorkerService Service { get; set; }
            public ScheduledWorkerServiceSettings Settings { get; set; }
            public DateTimeOffset NextRun { get; set; }

            public ScheduledWorkerServiceContext(ScheduledWorkerService service, ScheduledWorkerServiceSettings settings, DateTimeOffset now)
            {
                Service = service;
                Settings = settings;
                NextRun = now + settings.TriggerInterval;
            }
        }

    }
}
