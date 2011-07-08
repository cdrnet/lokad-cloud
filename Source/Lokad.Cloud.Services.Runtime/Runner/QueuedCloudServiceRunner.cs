using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Lokad.Cloud.Services.Framework;
using Lokad.Cloud.Services.Management.Settings;

namespace Lokad.Cloud.Services.Runtime.Runner
{
    internal class QueuedCloudServiceRunner
    {
        private readonly List<ServiceWithSettings<UntypedQueuedCloudService, QueuedCloudServiceSettings>> _services;
        private int _nextIndex;

        public QueuedCloudServiceRunner(IEnumerable<ServiceWithSettings<UntypedQueuedCloudService, QueuedCloudServiceSettings>> services)
        {
            _services = services.ToList();
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
