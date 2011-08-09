﻿#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml;
using Lokad.Cloud.Console.WebRole.Helpers;
using Lokad.Cloud.Console.WebRole.Models.Queues;
using Lokad.Cloud.Console.WebRole.Models.Services;
using Lokad.Cloud.Services.Management;
using Lokad.Cloud.Services.Management.Application;
using Lokad.Cloud.Storage;
using Lokad.Cloud.Storage.Shared;

namespace Lokad.Cloud.Console.WebRole.Framework.Services
{
    public class AppDefinitionWithLiveDataProvider
    {
        const string FailingMessagesStoreName = "failing-messages";
        private readonly CloudStorageProviders _storage;

        public AppDefinitionWithLiveDataProvider(CloudStorageProviders storage)
        {
            _storage = storage;
        }

        public ServicesModel QueryServices()
        {
            var serviceManager = new CloudServices(_storage.NeutralBlobStorage);
            var services = serviceManager.GetServices();

            var inspector = new CloudApplicationInspector(_storage);
            var applicationDefinition = inspector.Inspect();

            if (!applicationDefinition.HasValue)
            {
                return new ServicesModel
                    {
                        QueuedCloudServices = new QueueServiceModel[0],
                        ScheduledCloudServices = new CloudServiceInfo[0],
                        ScheduledWorkerServices = new CloudServiceInfo[0],
                        DaemonServices = new CloudServiceInfo[0]
                    };
            }

            var appDefinition = applicationDefinition.Value;

            var queuedCloudServices = services.Join(
                appDefinition.QueuedCloudServices,
                s => s.ServiceName,
                d => d.TypeName,
                (s, d) => new QueueServiceModel { ServiceName = s.ServiceName, IsStarted = s.IsStarted, Definition = d }).ToArray();

            var scheduledCloudServices = services.Where(s => appDefinition.ScheduledCloudServices.Any(ads => ads.TypeName.StartsWith(s.ServiceName))).ToArray();
            var scheduledWorkerServices = services.Where(s => appDefinition.ScheduledWorkerServices.Any(ads => ads.TypeName.StartsWith(s.ServiceName))).ToArray();
            var daemonServices = services.Where(s => appDefinition.DaemonServices.Any(ads => ads.TypeName.StartsWith(s.ServiceName))).ToArray();

            return new ServicesModel
                {
                    QueuedCloudServices = queuedCloudServices,
                    ScheduledCloudServices = scheduledCloudServices,
                    ScheduledWorkerServices = scheduledWorkerServices,
                    DaemonServices = daemonServices
                };
        }

        public QueuesModel QueryQueues()
        {
            var queues = _storage.NeutralQueueStorage;
            var inspector = new CloudApplicationInspector(_storage);
            var applicationDefinition = inspector.Inspect();

            var failingMessages = queues.ListPersisted(FailingMessagesStoreName)
                .Select(key => queues.GetPersisted(FailingMessagesStoreName, key))
                .Where(m => m.HasValue)
                .Select(m => m.Value)
                .OrderByDescending(m => m.PersistenceTime)
                .Take(50)
                .ToList();

            return new QueuesModel
                {
                    Queues = queues.List(null).Select(queueName => new AzureQueue
                    {
                        QueueName = queueName,
                        MessageCount = queues.GetApproximateCount(queueName),
                        Latency = queues.GetApproximateLatency(queueName).Convert(ts => ts.PrettyFormat(), string.Empty),
                        Services = applicationDefinition.Convert(cd => cd.QueuedCloudServices.Where(d => d.QueueName == queueName).ToArray(), new QueuedCloudServiceDefinition[0])
                    }).ToArray(),

                    HasQuarantinedMessages = failingMessages.Count > 0,

                    Quarantine = failingMessages
                        .GroupBy(m => m.QueueName)
                        .Select(group => new AzureQuarantineQueue
                        {
                            QueueName = group.Key,
                            Messages = group.OrderByDescending(m => m.PersistenceTime)
                                .Select(m => new AzureQuarantineMessage
                                {
                                    Inserted = FormatUtil.TimeOffsetUtc(m.InsertionTime.UtcDateTime),
                                    Persisted = FormatUtil.TimeOffsetUtc(m.PersistenceTime.UtcDateTime),
                                    Reason = HttpUtility.HtmlEncode(m.Reason),
                                    Content = FormatQuarantinedLogEntryXmlContent(m),
                                    Key = m.Key,
                                    HasData = m.IsDataAvailable,
                                    HasXml = m.DataXml.HasValue
                                })
                                .ToArray()
                        })
                        .ToArray()
                };
        }

        static string FormatQuarantinedLogEntryXmlContent(PersistedMessage message)
        {
            if (!message.IsDataAvailable || !message.DataXml.HasValue)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = Environment.NewLine,
                NewLineHandling = NewLineHandling.Replace,
                OmitXmlDeclaration = true
            };

            using (var writer = XmlWriter.Create(sb, settings))
            {
                message.DataXml.Value.WriteTo(writer);
                writer.Flush();
            }

            return HttpUtility.HtmlEncode(sb.ToString());
        }
    }
}