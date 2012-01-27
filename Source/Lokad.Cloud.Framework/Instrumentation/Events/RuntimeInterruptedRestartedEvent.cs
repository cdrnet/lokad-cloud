#region Copyright (c) Lokad 2011-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Xml.Linq;
using Lokad.Cloud.AppHost.Framework;

namespace Lokad.Cloud.Instrumentation.Events
{
    public class RuntimeInterruptedRestartedEvent : IRuntimeEvent
    {
        public EventLevel Level { get { return EventLevel.Trace; } }
        public CellLifeIdentity Cell { get; private set; }
        public string ServiceName { get; set; }

        public RuntimeInterruptedRestartedEvent(CellLifeIdentity cell, string serviceName)
        {
            Cell = cell;
            ServiceName = serviceName;
        }

        public string Describe()
        {
            return string.Format("Runtime execution was forcibly interrupted in service {0} on cell {1} of solution {2} on {3}. The Runtime will be restarted.",
                ServiceName, Cell.CellName, Cell.SolutionName, Cell.Host.WorkerName);
        }

        public XElement DescribeMeta()
        {
            return new XElement("Meta",
                new XElement("Component", "Lokad.Cloud.Framework"),
                new XElement("Event", "RuntimeInterruptedRestartedEvent"),
                new XElement("AppHost",
                    new XElement("Host", Cell.Host.WorkerName),
                    new XElement("Solution", Cell.SolutionName),
                    new XElement("Cell", Cell.CellName)),
                new XElement("Service", ServiceName));
        }
    }
}
