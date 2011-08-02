#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Linq;

namespace Lokad.Cloud.Services.Framework.Runner
{
    // TODO: Consider random permutations

    internal class QueuedCloudServiceRunner : CommonServiceRunner<UntypedQueuedCloudService>
    {
        private readonly List<QueuedCloudServiceContext> _services;
        private int _nextIndex;

        public QueuedCloudServiceRunner(List<ServiceWithSettings<UntypedQueuedCloudService>> services)
            : base(services)
        {
            _services = services.Select(s => new QueuedCloudServiceContext(s.Service, s.ServiceXml)).ToList();
        }

        public bool RunSingle(CancellationToken cancellationToken)
        {
            if (_services.Count == 0 || cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            var service = _services[_nextIndex];
            _nextIndex = (_nextIndex + 1) % _services.Count;

            var start = DateTimeOffset.UtcNow;
            var hadAnyMessage = false;
            while (!cancellationToken.IsCancellationRequested && DateTimeOffset.UtcNow.Subtract(start) < service.ContinueProcessingIfMessagesAvailable)
            {
                var hadMessage = service.Service.TryGetMessageAndProcess(service.QueueName, service.VisibilityTimeout, service.MaxProcessingTrials, cancellationToken);
                hadAnyMessage |= hadMessage;
                if (!hadMessage)
                {
                    break;
                }
            }

            return hadAnyMessage;
        }

        class QueuedCloudServiceContext
        {
            public UntypedQueuedCloudService Service { get; private set; }
            public string QueueName { get; private set; }
            public TimeSpan VisibilityTimeout { get; private set; }
            public TimeSpan ContinueProcessingIfMessagesAvailable { get; private set; }
            public int MaxProcessingTrials { get; private set; }

            public QueuedCloudServiceContext(UntypedQueuedCloudService service, XElement serviceSettings)
            {
                Service = service;
                QueueName = serviceSettings.SettingsElementAttributeValue("Queue", "name");
                VisibilityTimeout = TimeSpan.Parse(serviceSettings.SettingsElementAttributeValue("Timing", "invisibility"));
                ContinueProcessingIfMessagesAvailable = TimeSpan.Parse(serviceSettings.SettingsElementAttributeValue("Timing", "continueFor"));
                MaxProcessingTrials = Int32.Parse(serviceSettings.SettingsElementAttributeValue("Quarantine", "maxTrials"));
            }
        }
    }
}
