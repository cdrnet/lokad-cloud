#region Copyright (c) Lokad 2011-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Xml.Linq;

namespace Lokad.Cloud.Instrumentation.Events
{
    public class RuntimeStoppedEvent : IRuntimeEvent
    {
        public EventLevel Level { get { return EventLevel.Trace; } }
        public HostInfo Host { get; private set; }

        public RuntimeStoppedEvent(HostInfo host)
        {
            Host = host;
        }

        public string Describe()
        {
            return string.Format("Runtime: stopped in {0} cell of {1} solution on {2}.",
                Host.CellName, Host.SolutionName, Host.WorkerName);
        }

        public XElement DescribeMeta()
        {
            return new XElement("Meta",
                new XElement("Component", "Lokad.Cloud.Framework"),
                new XElement("Event", "RuntimeStoppedEvent"),
                new XElement("AppHost",
                    new XElement("Host", Host.WorkerName),
                    new XElement("Solution", Host.SolutionName),
                    new XElement("Cell", Host.CellName)));
        }
    }
}
