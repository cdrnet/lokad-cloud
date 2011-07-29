#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using System.Collections.Generic;
using Lokad.Cloud.Services.Management.Application;

namespace Lokad.Cloud.Services.Management.Settings
{
    public static class DefaultSettings
    {
        private const string DefaultCellName = "default";
        private static readonly TimeSpan DefaultProcessingTimeout = new TimeSpan(1, 58, 0);
        private static readonly TimeSpan DefaultVisibilityTimeout = new TimeSpan(2, 0, 0);
        private static readonly TimeSpan DefaultTriggerInterval = new TimeSpan(1, 0, 0);
        private static readonly TimeSpan DefaultContinueProcessingTimeSpan = new TimeSpan(0, 1, 0);

        /// <summary>
        /// Create default settings for the provided scheduled cloud service definition
        /// </summary>
        public static QueuedCloudServiceSettings For(QueuedCloudServiceDefinition queuedCloudService)
        {
            return new QueuedCloudServiceSettings
                {
                    TypeName = queuedCloudService.TypeName,
                    CellAffinity = new List<string> { DefaultCellName },
                    ProcessingTimeout = DefaultProcessingTimeout,
                    MessageTypeName = queuedCloudService.MessageTypeName,
                    QueueName = queuedCloudService.QueueName,
                    VisibilityTimeout = DefaultVisibilityTimeout,
                    ContinueProcessingIfMessagesAvailable = DefaultContinueProcessingTimeSpan,
                    MaxProcessingTrials = 5
                };
        }

        /// <summary>
        /// Create default settings for the provided scheduled cloud service definition
        /// </summary>
        public static ScheduledCloudServiceSettings For(ScheduledCloudServiceDefinition scheduledCloudService)
        {
            return new ScheduledCloudServiceSettings
            {
                TypeName = scheduledCloudService.TypeName,
                CellAffinity = new List<string> { DefaultCellName },
                ProcessingTimeout = DefaultProcessingTimeout,
                TriggerInterval = DefaultTriggerInterval
            };
        }

        /// <summary>
        /// Create default settings for the provided scheduled worker service definition
        /// </summary>
        public static ScheduledWorkerServiceSettings For(ScheduledWorkerServiceDefinition scheduledWorkerService)
        {
            return new ScheduledWorkerServiceSettings
            {
                TypeName = scheduledWorkerService.TypeName,
                CellAffinity = new List<string> { DefaultCellName },
                ProcessingTimeout = DefaultProcessingTimeout,
                TriggerInterval = DefaultTriggerInterval
            };
        }

        /// <summary>
        /// Create default settings for the provided daemon service definition
        /// </summary>
        public static DaemonServiceSettings For(DaemonServiceDefinition daemonService)
        {
            return new DaemonServiceSettings
            {
                TypeName = daemonService.TypeName,
                CellAffinity = new List<string> { DefaultCellName }
            };
        }

        /// <summary>
        /// Create default settings for the provided cloud application definition
        /// </summary>
        public static CloudServicesSettings For(CloudApplicationDefinition definition)
        {
            return new CloudServicesSettings
                {
                    QueuedCloudServices = definition.QueuedCloudServices.Select(For).ToList(),
                    ScheduledCloudServices = definition.ScheduledCloudServices.Select(For).ToList(),
                    ScheduledWorkerServices = definition.ScheduledWorkerServices.Select(For).ToList(),
                    DaemonServices = definition.DaemonServices.Select(For).ToList()
                };
        }

        /// <summary>
        /// Extend the provided settings to support the provided new cloud application definition
        /// </summary>
        /// <returns><c>true</c> if the settings have been changed, <c>false</c> otherwise.</returns>
        public static bool ExtendForNewServicesIn(CloudServicesSettings settings, CloudApplicationDefinition definition)
        {
            bool changed = false;

            if (settings.QueuedCloudServices == null) settings.QueuedCloudServices = new List<QueuedCloudServiceSettings>();
            foreach (var queuedCloudService in definition.QueuedCloudServices.Where(d => !settings.QueuedCloudServices.Exists(s => s.TypeName == d.TypeName)))
            {
                changed = true;
                settings.QueuedCloudServices.Add(DefaultSettings.For(queuedCloudService));
            }

            if (settings.ScheduledCloudServices == null) settings.ScheduledCloudServices = new List<ScheduledCloudServiceSettings>();
            foreach (var scheduledCloudService in definition.ScheduledCloudServices.Where(d => !settings.ScheduledCloudServices.Exists(s => s.TypeName == d.TypeName)))
            {
                changed = true;
                settings.ScheduledCloudServices.Add(DefaultSettings.For(scheduledCloudService));
            }

            if (settings.ScheduledWorkerServices == null) settings.ScheduledWorkerServices = new List<ScheduledWorkerServiceSettings>();
            foreach (var scheduledWorkerService in definition.ScheduledWorkerServices.Where(d => !settings.ScheduledWorkerServices.Exists(s => s.TypeName == d.TypeName)))
            {
                changed = true;
                settings.ScheduledWorkerServices.Add(DefaultSettings.For(scheduledWorkerService));
            }

            if (settings.DaemonServices == null) settings.DaemonServices = new List<DaemonServiceSettings>();
            foreach (var daemonService in definition.DaemonServices.Where(d => !settings.DaemonServices.Exists(s => s.TypeName == d.TypeName)))
            {
                changed = true;
                settings.DaemonServices.Add(DefaultSettings.For(daemonService));
            }

            return changed;
        }
    }
}
