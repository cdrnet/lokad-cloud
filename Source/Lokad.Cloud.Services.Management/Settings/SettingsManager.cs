#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.Services.Management.Application;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.Services.Management.Settings
{
    public class SettingsManager
    {
        private const string ContainerName = "lokad-cloud-services";
        private const string ServiceSettingsBlobName = "services.settings.lokadcloud";

        private readonly CloudStorageProviders _storage;
        private string _lastKnownEtag;

        public SettingsManager(CloudStorageProviders storage)
        {
            _storage = storage;
        }

        public bool HaveSettingsChanged()
        {
            return _storage.NeutralBlobStorage.GetBlobEtag(ContainerName, ServiceSettingsBlobName) != _lastKnownEtag;
        }

        public void ForgetLastKnownSettings()
        {
            _lastKnownEtag = null;
        }

        /// <summary>
        /// Load the current settings and extend them to support the provided new cloud application if needed.
        /// </summary>
        public Maybe<CloudServicesSettings> LoadSettingsAndExtendToApplication(CloudApplicationDefinition definition)
        {
            _storage.NeutralBlobStorage.UpsertBlobOrSkip<CloudServicesSettings>(ContainerName, ServiceSettingsBlobName,
                insert: () => DefaultSettings.For(definition),
                update: settings => DefaultSettings.ExtendForNewServicesIn(settings, definition) ? settings : Maybe<CloudServicesSettings>.Empty);

            // TODO: Potential race, fix blob storage provider to support returning both resulting value and etag (of previous state if skipped)

            return _storage.NeutralBlobStorage.GetBlob<CloudServicesSettings>(ContainerName, ServiceSettingsBlobName, out _lastKnownEtag);
        }

        /// <summary>
        /// Replace the current settings with the provided new settings.
        /// </summary>
        public void ReplaceSettings(CloudServicesSettings newSettings)
        {
            _storage.NeutralBlobStorage.PutBlob(ContainerName, ServiceSettingsBlobName, newSettings, true, out _lastKnownEtag);
        }
    }
}
