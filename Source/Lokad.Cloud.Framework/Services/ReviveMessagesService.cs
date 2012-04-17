#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.ServiceFabric;

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
        }
    }
}
