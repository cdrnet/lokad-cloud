#region Copyright (c) Lokad 2011-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Xml.Linq;
using Lokad.Cloud.Jobs;

namespace Lokad.Cloud.Instrumentation.Events
{
    /// <summary>
    /// Raised whenever a job has succeeded.
    /// </summary>
    public class JobSucceededEvent : IRuntimeEvent
    {
        public EventLevel Level { get { return EventLevel.Trace; } }
        public Job Job { get; private set; }
        public DateTimeOffset Timestamp { get; private set; }

        public JobSucceededEvent(Job job, DateTimeOffset timestamp)
        {
            Job = job;
            Timestamp = timestamp;
        }

        public string Describe()
        {
            return string.Format("Runtime: Job {0} has succeeded at {1}.", Job.JobId, Timestamp);
        }

        public XElement DescribeMeta()
        {
            return new XElement("Meta",
                new XElement("Component", "Lokad.Cloud.Framework"),
                new XElement("Event", "JobSucceededEvent"),
                new XElement("Job", Job.JobId));
        }
    }
}
