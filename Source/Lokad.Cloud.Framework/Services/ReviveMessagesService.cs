#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.ServiceFabric;
using Lokad.Cloud.Storage;

// HACK: the delayed queue service does not provide a scalable iteration pattern.
// (single instance iterating over the delayed message)

namespace Lokad.Cloud.Services
{
    /// <summary>Routinely checks for dead or expired-delayed messages that needs to
    /// be put in queue for immediate consumption.</summary>
    [ScheduledServiceSettings(
        AutoStart = true,
        Description = "Checks for dead and expired delayed messages to be put in regular queue.",
        TriggerInterval = 15)] // 15s
    public class ReviveMessagesService : ScheduledService
    {
        protected override void StartOnSchedule()
        {
            Queues.ReviveMessages();

            // TODO (ruegg, 2011-08-31): legacy - to be ported into ReviveMessages later:

            // lazy enumeration over the delayed messages
            foreach (var parsedName in Blobs.ListBlobNames(new DelayedMessageName()))
            {
                if (DateTimeOffset.UtcNow <= parsedName.TriggerTime)
                {
                    // delayed messages are iterated in date-increasing order
                    // as soon a non-expired delayed message is encountered
                    // just stop the process.
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
