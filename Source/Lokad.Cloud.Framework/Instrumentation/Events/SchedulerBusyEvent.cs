#region Copyright (c) Lokad 2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Xml.Linq;

namespace Lokad.Cloud.Instrumentation.Events
{
    /// <summary>
    /// Raised whenever the runtime scheduler becomes busy.
    /// </summary>
    public class SchedulerBusyEvent : IRuntimeEvent
    {
        public EventLevel Level { get { return EventLevel.Trace; } }
        public DateTimeOffset Timestamp { get; private set; }

        public SchedulerBusyEvent(DateTimeOffset timestamp)
        {
            Timestamp = timestamp;
        }

        public string Describe()
        {
            return string.Format("Runtime: Scheduler is back working again since {0}.", Timestamp);
        }

        public XElement DescribeMeta()
        {
            return new XElement("Meta",
                new XElement("Component", "Lokad.Cloud.Framework"),
                new XElement("Event", "SchedulerBusyEvent"));
        }
    }
}
