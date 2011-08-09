#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Threading;
using Lokad.Cloud.Services.Framework.Logging;
using Lokad.Cloud.Services.Framework.Storage;
using Lokad.Cloud.Storage;

// HACK: the delayed queue service does not provide a scalable iteration pattern.
// (single instance iterating over the delayed message)

namespace Lokad.Cloud.Services.Framework.SystemServices
{
    /// <summary>
    /// Routinely checks for expired delayed messages that needs to
    /// be put in queue for immediate consumption.
    /// </summary>
    [ScheduledCloudServiceDefaultSettings(TriggerIntervalSeconds = 15)]
    public class DelayedQueueService : ScheduledCloudService
    {
        public override void OnSchedule(DateTimeOffset scheduledTime, CancellationToken cancellationToken)
        {
            // lazy enumeration over the delayed messages
            foreach (var parsedName in Blobs.ListBlobNames(new DelayedMessageName()))
            {
                // delayed messages are iterated in date-increasing order
                // as soon a non-expired delayed message is encountered
                // just stop the process.

                if (DateTimeOffset.UtcNow <= parsedName.TriggerTime
                    || cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var dm = Blobs.GetBlob(parsedName);
                if (!dm.HasValue)
                {
                    Log.WarnFormat("Deserialization failed for delayed message {0}, message was dropped.", parsedName.Identifier);
                    continue;
                }

                Queues.Put(dm.Value.QueueName, dm.Value.InnerMessage);
                Blobs.DeleteBlobIfExist(parsedName);
            }
        }
    }
}
