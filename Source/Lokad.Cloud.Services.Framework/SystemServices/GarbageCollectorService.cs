#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Threading;
using Lokad.Cloud.ServiceFabric;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.Services.Framework.SystemServices
{
    /// <summary>
    /// Garbage collects temporary items stored in the <see cref="CloudService.TemporaryContainer"/>.
    /// </summary>
    /// <remarks>
    /// The container <see cref="CloudService.TemporaryContainer"/> is handy to
    /// store non-persistent data, typically state information concerning ongoing
    /// processing.
    /// </remarks>
    [ScheduledServiceSettings(TriggerInterval = 300)] // by default 1 execution every 5min
    public class GarbageCollectorService : ScheduledCloudService
    {
        static TimeSpan MaxExecutionTime { get { return TimeSpan.FromMinutes(10); } }

        public override void OnSchedule(DateTimeOffset scheduledTime, CancellationToken cancellationToken)
        {
            const string containerName = TemporaryBlobName<object>.DefaultContainerName;
            var executionExpiration = DateTimeOffset.UtcNow.Add(MaxExecutionTime);

            // lazy enumeration over the overflowing messages
            foreach (var blobName in Blobs.ListBlobNames(containerName))
            {
                var parsedName = UntypedBlobName.Parse<TemporaryBlobName<object>>(blobName);

                // overflowing messages are iterated in date-increasing order
                // as soon a non-expired overflowing message is encountered
                // just stop the process.

                if (DateTimeOffset.UtcNow <= parsedName.Expiration
                    || DateTimeOffset.UtcNow > executionExpiration
                    || cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                // if the overflowing message is expired, delete it
                Blobs.DeleteBlobIfExist(containerName, blobName);
            }
        }
    }
}
