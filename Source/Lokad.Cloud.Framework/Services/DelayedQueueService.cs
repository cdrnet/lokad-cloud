﻿#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;

// HACK: the delayed queue service does not provide a scalable iteration pattern.
// (single instance iterating over the delayed message)

namespace Lokad.Cloud.Services
{
	/// <summary>Routinely checks for expired delayed messages that needs to
	/// be put in queue for immediate consumption.</summary>
	[ScheduledServiceSettings(
		AutoStart = true,
		Description = "Checks for expired delayed messages to be put in regular queue.",
		TriggerInterval = 15)] // 15s
	public class DelayedQueueService : ScheduledService
	{
		protected override void StartOnSchedule()
		{
			// lazy enumeration over the delayed messages
			string nullPrefix = null;
			foreach (var blobName in BlobStorage.List<DelayedMessageName>(nullPrefix))
			{
				var parsedName = BaseBlobName.Parse<DelayedMessageName>(blobName);

				// if the overflowing message is expired, delete it
				if (DateTime.UtcNow > parsedName.TriggerTime)
				{
					var dm = BlobStorage.GetBlobOrDelete<DelayedMessage>(parsedName);
					if (!dm.HasValue)
					{
						Log.WarnFormat("Deserialization failed for delayed message {0}, message was dropped.", parsedName.Identifier);
						continue;
					}

					QueueStorage.Put(dm.Value.QueueName, dm.Value.InnerMessage);
					BlobStorage.DeleteBlob(parsedName);
				}
				else
				{
					// delayed messages are iterated in date-increasing order
					// as soon a non-expired delayed message is encountered
					// just stop the process.
					break;
				}
			}
		}
	}
}
