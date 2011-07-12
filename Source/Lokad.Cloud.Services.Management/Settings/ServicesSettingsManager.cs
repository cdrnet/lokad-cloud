#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Lokad.Cloud.Services.Management.Application;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.Services.Management.Settings
{
    public class ServicesSettingsManager
    {
        private const string ContainerName = "lokad-cloud-services";
        private const string ServiceSettingsBlobName = "services.settings.lokadcloud";
        private const string DefaultCellName = "default";

        private readonly CloudStorageProviders _storage;

        public ServicesSettingsManager(CloudStorageProviders storage)
        {
            _storage = storage;
        }

        public bool HaveSettingsChanged(string expectedEtag)
        {
            return _storage.NeutralBlobStorage.GetBlobEtag(ContainerName, ServiceSettingsBlobName) != expectedEtag;
        }

        /// <summary>
        /// Load the current settings and extend them to support the provided new cloud application if needed.
        /// </summary>
        public Maybe<CloudServicesSettings> UpdateAndLoadSettings(CloudApplicationDefinition definition, out string etag)
        {
            _storage.NeutralBlobStorage.UpsertBlobOrSkip<CloudServicesSettings>(ContainerName, ServiceSettingsBlobName,
                insert: () => InitializeSettingsForApplication(definition),
                update: settings => ExtendSettingsForApplication(settings, definition) ? settings : Maybe<CloudServicesSettings>.Empty);

            return _storage.NeutralBlobStorage.GetBlob<CloudServicesSettings>(ContainerName, ServiceSettingsBlobName, out etag);
        }

        /// <summary>
        /// Create default settings for the provided queued cloud service definition
        /// </summary>
        public static QueuedCloudServiceSettings InitializeSettingsForService(QueuedCloudServiceDefinition queuedCloudService)
        {
            return new QueuedCloudServiceSettings
                {
                    TypeName = queuedCloudService.TypeName,
                    CellAffinity = new List<string> { DefaultCellName },
                    ProcessingTimeout = new TimeSpan(1, 58, 0),
                    MessageTypeName = queuedCloudService.MessageTypeName,
                    QueueName = queuedCloudService.QueueName,
                    VisibilityTimeout = TimeSpan.FromHours(2),
                    ContinueProcessingIfMessagesAvailable = TimeSpan.FromMinutes(1),
                    MaxProcessingTrials = 5
                };
        }

        /// <summary>
        /// Create default settings for the provided scheduled cloud service definition
        /// </summary>
        public static ScheduledCloudServiceSettings InitializeSettingsForService(ScheduledCloudServiceDefinition scheduledCloudService)
        {
            return new ScheduledCloudServiceSettings
                {
                    TypeName = scheduledCloudService.TypeName,
                    CellAffinity = new List<string> { DefaultCellName },
                    ProcessingTimeout = new TimeSpan(1, 58, 0),
                    TriggerInterval = TimeSpan.FromHours(1)
                };
        }

        /// <summary>
        /// Create default settings for the provided scheduled worker service definition
        /// </summary>
        public static ScheduledWorkerServiceSettings InitializeSettingsForService(ScheduledWorkerServiceDefinition scheduledWorkerService)
        {
            return new ScheduledWorkerServiceSettings
                {
                    TypeName = scheduledWorkerService.TypeName,
                    CellAffinity = new List<string> { DefaultCellName },
                    ProcessingTimeout = new TimeSpan(1, 58, 0),
                    TriggerInterval = TimeSpan.FromHours(1)
                };
        }

        /// <summary>
        /// Create default settings for the provided daemon service definition
        /// </summary>
        public static DaemonServiceSettings InitializeSettingsForService(DaemonServiceDefinition daemonService)
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
        public static CloudServicesSettings InitializeSettingsForApplication(CloudApplicationDefinition definition)
        {
            var settings = new CloudServicesSettings();
            ExtendSettingsForApplication(settings, definition);
            return settings;
        }

        /// <summary>
        /// Extend the provided settings to support the provided new cloud application definition
        /// </summary>
        /// <returns><c>true</c> if the settings have been changed, <c>false</c> otherwise.</returns>
        public static bool ExtendSettingsForApplication(CloudServicesSettings settings, CloudApplicationDefinition definition)
        {
            bool changed = false;

            if (settings.QueuedCloudServices == null) settings.QueuedCloudServices = new List<QueuedCloudServiceSettings>();
            foreach (var queuedCloudService in definition.QueuedCloudServices.Where(d => !settings.QueuedCloudServices.Exists(s => s.TypeName == d.TypeName)))
            {
                changed = true;
                settings.QueuedCloudServices.Add(InitializeSettingsForService(queuedCloudService));
            }

            if (settings.ScheduledCloudServices == null) settings.ScheduledCloudServices = new List<ScheduledCloudServiceSettings>();
            foreach (var scheduledCloudService in definition.ScheduledCloudServices.Where(d => !settings.ScheduledCloudServices.Exists(s => s.TypeName == d.TypeName)))
            {
                changed = true;
                settings.ScheduledCloudServices.Add(InitializeSettingsForService(scheduledCloudService));
            }

            if (settings.ScheduledWorkerServices == null) settings.ScheduledWorkerServices = new List<ScheduledWorkerServiceSettings>();
            foreach (var scheduledWorkerService in definition.ScheduledWorkerServices.Where(d => !settings.ScheduledWorkerServices.Exists(s => s.TypeName == d.TypeName)))
            {
                changed = true;
                settings.ScheduledWorkerServices.Add(InitializeSettingsForService(scheduledWorkerService));
            }

            if (settings.DaemonServices == null) settings.DaemonServices = new List<DaemonServiceSettings>();
            foreach (var daemonService in definition.DaemonServices.Where(d => !settings.DaemonServices.Exists(s => s.TypeName == d.TypeName)))
            {
                changed = true;
                settings.DaemonServices.Add(InitializeSettingsForService(daemonService));
            }

            return changed;
        }
    }
}
