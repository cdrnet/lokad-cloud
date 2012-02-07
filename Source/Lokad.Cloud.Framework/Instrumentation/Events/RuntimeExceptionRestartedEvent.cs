#region Copyright (c) Lokad 2011-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Xml.Linq;

namespace Lokad.Cloud.Instrumentation.Events
{
    public class RuntimeExceptionRestartedEvent : IRuntimeEvent
    {
        public EventLevel Level { get { return EventLevel.Error; } }
        public HostInfo Host { get; private set; }
        public string ServiceName { get; set; }
        public Exception Exception { get; set; }

        public RuntimeExceptionRestartedEvent(HostInfo host, string serviceName, Exception exception)
        {
            Host = host;
            ServiceName = serviceName;
            Exception = exception;
        }

        public string Describe()
        {
            return string.Format("Runtime: an unhandled {0} occured in service {1} on cell {2} of solution {3} on {4}. The Runtime will be restarted.",
                Exception.GetType().Name, ServiceName, Host.CellName, Host.SolutionName, Host.WorkerName);
        }

        public XElement DescribeMeta()
        {
            var meta = new XElement("Meta",
                new XElement("Component", "Lokad.Cloud.Framework"),
                new XElement("Event", "RuntimeExceptionRestartedEvent"),
                new XElement("AppHost",
                    new XElement("Host", Host.WorkerName),
                    new XElement("Solution", Host.SolutionName),
                    new XElement("Cell", Host.CellName)),
                new XElement("Service", ServiceName));

            if (Exception != null)
            {
                meta.Add(new XElement("Exception",
                    new XAttribute("typeName", Exception.GetType().FullName),
                    new XAttribute("message", Exception.Message),
                    Exception.ToString()));
            }

            return meta;
        }
    }
}
