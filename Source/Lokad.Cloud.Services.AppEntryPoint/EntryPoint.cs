#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.Services.AppEntryPoint.Util;
using Lokad.Cloud.Services.Framework;
using Lokad.Cloud.Services.Framework.Runner;

namespace Lokad.Cloud.Services.AppEntryPoint
{
    public class EntryPoint : IApplicationEntryPoint
    {
        private IDeploymentReader _deploymentReader;

        public void Run(XElement settings, IDeploymentReader deploymentReader, IApplicationEnvironment environment, CancellationToken cancellationToken)
        {
            _deploymentReader = deploymentReader;

            var config = ReadConfig(settings, deploymentReader);
            var servicesXml = ReadServicesXml(settings, deploymentReader);

            var serviceXmlsByType = servicesXml.Elements("Service")
                .ToLookup(service => service.AttributeValue("type"));

            using (var container = new ServiceContainer(config, environment))
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var runner = new ServiceRunner();
                    runner.Run(
                        container.ResolveServices<UntypedQueuedCloudService>(serviceXmlsByType["QueuedCloudService"]),
                        container.ResolveServices<ScheduledCloudService>(serviceXmlsByType["ScheduledCloudService"]),
                        container.ResolveServices<ScheduledWorkerService>(serviceXmlsByType["ScheduledWorkerService"]),
                        container.ResolveServices<DaemonService>(serviceXmlsByType["DaemonService"]),
                        cancellationToken);
                }
            }
        }

        public void ApplyChangedSettings(XElement newSettings)
        {
            var newConfig = ReadConfig(newSettings, _deploymentReader);
            var newServicesXml = ReadServicesXml(newSettings, _deploymentReader);
            
            // TODO: implement
        }

        byte[] ReadConfig(XElement settings, IDeploymentReader reader)
        {
            var tag = settings.Element("Config");
            if (tag == null)
            {
                return new byte[0];
            }

            var name = tag.Attribute("name");
            if (name != null)
            {
                return reader.GetItem<byte[]>(name.Value);
            }

            var value = tag.Value;
            if (!string.IsNullOrEmpty(value))
            {
                return Convert.FromBase64String(value.Trim());
            }

            return new byte[0];
        }

        XElement ReadServicesXml(XElement settings, IDeploymentReader reader)
        {
            var tag = settings.Element("Services");
            if (tag == null)
            {
                return new XElement("Services");
            }

            var name = tag.Attribute("name");
            if (name != null)
            {
                return reader.GetItem<XElement>(name.Value);
            }

            return tag;
        }
    }
}
