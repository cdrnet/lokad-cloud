﻿#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Lokad.Cloud.Services.Framework.Logging;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.Services.Framework
{
    public abstract class QueuedCloudService<TMessage> : UntypedQueuedCloudService
    {
        // Actions to be implemented by implementors
        public abstract void OnQueueMessage(TMessage message, CancellationToken cancellationToken);

        public sealed override bool TryGetMessageAndProcess(string queueName, TimeSpan visibilityTimeout, int maxProcessingTrials, CancellationToken cancellationToken)
        {
            // 1 message at most
            var messages = Queues.Get<TMessage>(queueName, 1, visibilityTimeout, maxProcessingTrials).ToList();

            if (messages.Count == 1)
            {
                var message = messages[0];
                OnQueueMessage(message, cancellationToken);

                // Messages might have already been deleted by the 'Start' method.
                // It's OK, 'Delete' is idempotent.
                Queues.Delete(message);

                return true;
            }

            return false;
        }
    }

    public abstract class UntypedQueuedCloudService : ICloudService
    {
        // Actions to be implemented by implementors
        public virtual void Initialize(XElement userSettings) { }
        public virtual void OnStart() { }
        public virtual void OnStop() { }
        public abstract bool TryGetMessageAndProcess(string queueName, TimeSpan visibilityTimeout, int maxProcessingTrials, CancellationToken cancellationToken);

        // Injected by Runtime
        public ICloudEnvironment CloudEnvironment { get; set; }
        public ILogWriter Log { get; set; }
        public IBlobStorageProvider Blobs { get; set; }
        public IQueueStorageProvider Queues { get; set; }
        public ITableStorageProvider Tables { get; set; }

        CloudServiceType ICloudService.ServiceType
        {
            get { return CloudServiceType.QueuedCloudService; }
        }
    }
}