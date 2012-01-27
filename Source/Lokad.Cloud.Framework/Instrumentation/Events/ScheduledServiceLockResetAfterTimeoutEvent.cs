#region Copyright (c) Lokad 2011-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Xml.Linq;

namespace Lokad.Cloud.Instrumentation.Events
{
    public class ScheduledServiceLockResetAfterTimeoutEvent : IRuntimeEvent
    {
        public EventLevel Level { get { return EventLevel.Warning; } }
        public string ServiceName { get; set; }
        public string Owner { get; set; }
        public TimeSpan AfterBlockedFor { get; set; }

        public ScheduledServiceLockResetAfterTimeoutEvent(string serviceName, string owner, TimeSpan afterBlockedFor)
        {
            ServiceName = serviceName;
            Owner = owner;
            AfterBlockedFor = afterBlockedFor;
        }

        public string Describe()
        {
            return string.Format("ScheduledService {0}: Expired lease owned by {1} was reset after blocking for {2:0.0} minutes.",
                ServiceName, Owner, AfterBlockedFor.TotalMinutes);
        }

        public XElement DescribeMeta()
        {
            return new XElement("Meta",
                new XElement("Component", "Lokad.Cloud.Framework"),
                new XElement("Event", "ScheduledServiceLockResetAfterTimeoutEvent"),
                new XElement("Service", ServiceName));
        }
    }
}
