﻿#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Collections.Generic;
using System.Linq;
using Lokad.Cloud.Runtime;
using Lokad.Cloud.ServiceFabric;
using Lokad.Cloud.Storage;

// TODO: blobs are sequentially enumerated, performance issue
// if there are more than a few dozen services

namespace Lokad.Cloud.Services.Management
{
    /// <summary>Management facade for cloud services.</summary>
    public class CloudServices
    {
        readonly IBlobStorageProvider _blobProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudServices"/> class.
        /// </summary>
        public CloudServices(RuntimeProviders runtimeProviders)
        {
            _blobProvider = runtimeProviders.BlobStorage;
        }

        /// <summary>
        /// Enumerate infos of all cloud services.
        /// </summary>
        public List<CloudServiceInfo> GetServices()
        {
            // TODO: Redesign to make it self-contained (so that we don't need to pass the name as well)

            return _blobProvider.ListBlobNames(CloudServiceStateName.GetPrefix())
                .Select(name => System.Tuple.Create(name, _blobProvider.GetBlob(name)))
                .Where(pair => pair.Item2.HasValue)
                .Select(pair => new CloudServiceInfo
                    {
                        ServiceName = pair.Item1.ServiceName,
                        IsStarted = pair.Item2.Value == CloudServiceState.Started
                    })
                .ToList();
        }

        /// <summary>
        /// Gets info of one cloud service.
        /// </summary>
        public CloudServiceInfo GetService(string serviceName)
        {
            var blob = _blobProvider.GetBlob(new CloudServiceStateName(serviceName));
            return new CloudServiceInfo
                {
                    ServiceName = serviceName,
                    IsStarted = blob.Value == CloudServiceState.Started
                };
        }

        /// <summary>
        /// Enumerate the names of all cloud services.
        /// </summary>
        public List<string> GetServiceNames()
        {
            return _blobProvider.ListBlobNames(CloudServiceStateName.GetPrefix())
                .Select(reference => reference.ServiceName).ToList();
        }

        /// <summary>
        /// Enumerate the names of all user cloud services (system services are skipped).
        /// </summary>
        public List<string> GetUserServiceNames()
        {
            var systemServices =
                new[]
                    {
                        typeof(GarbageCollectorService),
                        typeof(DelayedQueueService),
                        typeof(MonitoringService),
                        typeof(MonitoringDataRetentionService),
                        typeof(AssemblyConfigurationUpdateService)
                    }
                    .Select(type => type.FullName)
                    .ToList();

            return GetServiceNames()
                .Where(service => !systemServices.Contains(service)).ToList();
        }

        /// <summary>
        /// Enable a cloud service
        /// </summary>
        public void EnableService(string serviceName)
        {
            _blobProvider.PutBlob(new CloudServiceStateName(serviceName), CloudServiceState.Started);
        }

        /// <summary>
        /// Disable a cloud service
        /// </summary>
        public void DisableService(string serviceName)
        {
            _blobProvider.PutBlob(new CloudServiceStateName(serviceName), CloudServiceState.Stopped);
        }

        /// <summary>
        /// Remove the state information of a cloud service
        /// </summary>
        public void ResetServiceState(string serviceName)
        {
            _blobProvider.DeleteBlobIfExist(new CloudServiceStateName(serviceName));
        }
    }
}
