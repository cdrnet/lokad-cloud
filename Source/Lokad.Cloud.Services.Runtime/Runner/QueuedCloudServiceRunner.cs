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
    internal class QueuedCloudServiceRunner : CommonServiceRunner
    {
        private readonly List<ServiceWithSettings<UntypedQueuedCloudService, QueuedCloudServiceSettings>> _services;
        private int _nextIndex;

        public QueuedCloudServiceRunner(List<ServiceWithSettings<UntypedQueuedCloudService, QueuedCloudServiceSettings>> services)
            : base(services.Select(s => s.Service))
        {
            _services = services;
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
            while (!cancellationToken.IsCancellationRequested && DateTimeOffset.UtcNow.Subtract(start) < service.Settings.ContinueProcessingIfMessagesAvailable)
            {
                var hadMessage = service.Service.TryGetMessageAndProcess(service.Settings.QueueName, service.Settings.VisibilityTimeout, service.Settings.MaxProcessingTrials, cancellationToken);
                hadAnyMessage |= hadMessage;
                if (!hadMessage)
                {
                    break;
                }
            }

            return hadAnyMessage;
        }
    }
}
