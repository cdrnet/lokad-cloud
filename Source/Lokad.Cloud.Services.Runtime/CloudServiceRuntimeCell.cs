#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Lokad.Cloud.Services.Framework;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.Services.Runtime
{
    /// <summary>
    /// A runtime cell is runtime unit that runs cloud services assigned to that cell
    /// until something unexpected happens. Multiple distinct cells can be active in
    /// parallel. A cell is assumed to be running in its own thread, or in its own
    /// long-running task. Cells can also be used in standalone scenarios e.g. for
    /// unit or integration testing or for off-line simulations.
    /// </summary>
    public class CloudServiceRuntimeCell
    {
        private const string ServiceSettingsContainerName = "lokad-cloud-services";
        private const string ServiceSettingsBlobName = "service-settings.lokadcloud";

        private readonly CloudStorageProviders _storage;
        private readonly IBlobStorageProvider _blobs;
        private readonly List<ICloudService> _allServices;

        private string _settingsEtag;

        public CloudServiceRuntimeCell(CloudStorageProviders storageProviders, IEnumerable<ICloudService> services)
        {
            _storage = storageProviders;
            _blobs = storageProviders.BlobStorage;

            _allServices = services.ToList();
        }

        /// <remarks>Only returns if something unexpected happens or when canceled.</remarks>
        public void Run(CancellationToken cancellationToken)
        {
            // TODO
        }

        //private void Setup()
        //{
        //    string newEtag;
        //    var settingsBlob = _blobs.GetBlobIfModified<CloudServicesSettings>(ServiceSettingsContainerName, ServiceSettingsBlobName, _settingsEtag, out newEtag);

        //    _blobs.UpsertBlobOrSkip<CloudServicesSettings>(ServiceSettingsContainerName, ServiceSettingsBlobName,
        //        insert: () => new CloudServicesSettings(),
        //        update: old => Maybe<CloudServicesSettings>.Empty);
        //}
    }
}
