#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.Services.App.Util;
using Lokad.Cloud.Services.Framework;

namespace Lokad.Cloud.Services.App
{
    public class EntryPoint : IApplicationEntryPoint
    {
        public void Run(XElement settings, IDeploymentReader deploymentReader, IApplicationEnvironment environment, CancellationToken cancellationToken)
        {
            var config = ReadConfig(settings, deploymentReader);
            var servicesSettings = ReadServicesSettings(settings, deploymentReader);

            var servicesSettingsByType = servicesSettings
                .Elements("Service")
                .ToLookup(service => service.AttributeValue("type"));

            using (var container = new ServiceContainer(config, environment))
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var runner = new Framework.Runner.ServiceRunner();
                    runner.Run(
                        container.ResolveServices<UntypedQueuedCloudService>(servicesSettingsByType["QueuedCloudService"]),
                        container.ResolveServices<ScheduledCloudService>(servicesSettingsByType["ScheduledCloudService"]),
                        container.ResolveServices<ScheduledWorkerService>(servicesSettingsByType["ScheduledWorkerService"]),
                        container.ResolveServices<DaemonService>(servicesSettingsByType["DaemonService"]),
                        cancellationToken);
                }
            }
        }

        public void ApplyChangedSettings(XElement newSettings)
        {
            
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

        XElement ReadServicesSettings(XElement settings, IDeploymentReader reader)
        {
            var tag = settings.Element("ServiceSettings");
            if (tag == null)
            {
                return new XElement("Services");
            }

            var name = tag.Attribute("name");
            if (name != null)
            {
                return reader.GetItem<XElement>(name.Value);
            }

            var child = tag.Element("Services");
            if (child != null)
            {
                return child;
            }

            return new XElement("Services");
        }
    }
}
