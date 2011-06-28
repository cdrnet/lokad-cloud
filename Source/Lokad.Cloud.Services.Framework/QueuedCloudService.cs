﻿#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.Storage;

namespace Lokad.Cloud.Services.Framework
{
    public abstract class QueuedCloudService<TMessage> : ICloudService
    {
        // Actions to be implemented by implementors
        public virtual void Initialize() { }
        public abstract void OnQueueMessage(TMessage message);

        // Injected by Runtime
        public IBlobStorageProvider Blobs { get; set; }
        public IQueueStorageProvider Queues { get; set; }
        public ITableStorageProvider Tables { get; set; }

        CloudServiceType ICloudService.ServiceType
        {
            get { return CloudServiceType.QueuedCloudService; }
        }
    }
}