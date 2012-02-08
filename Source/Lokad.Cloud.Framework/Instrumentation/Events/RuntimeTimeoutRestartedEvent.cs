#region Copyright (c) Lokad 2011-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Xml.Linq;

namespace Lokad.Cloud.Instrumentation.Events
{
    public class RuntimeTimeoutRestartedEvent : IRuntimeEvent
    {
        public EventLevel Level { get { return EventLevel.Warning; } }
        public HostInfo Host { get; private set; }
        public string ServiceName { get; set; }

        public RuntimeTimeoutRestartedEvent(HostInfo host, string serviceName)
        {
            Host = host;
            ServiceName = serviceName;
        }

        public string Describe()
        {
            return string.Format("Runtime: a timeout was raised in service {0} on {1} cell of {2} solution on {3}. The Runtime will be restarted.",
                ServiceName, Host.CellName, Host.SolutionName, Host.WorkerName);
        }

        public XElement DescribeMeta()
        {
            return new XElement("Meta",
                new XElement("Component", "Lokad.Cloud.Framework"),
                new XElement("Event", "RuntimeTimeoutRestartedEvent"),
                new XElement("AppHost",
                    new XElement("Host", Host.WorkerName),
                    new XElement("Solution", Host.SolutionName),
                    new XElement("Cell", Host.CellName)),
                new XElement("Service", ServiceName));
        }
    }
}
