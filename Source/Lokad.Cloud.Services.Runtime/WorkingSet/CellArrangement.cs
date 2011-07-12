#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using Lokad.Cloud.Services.Management.Settings;

namespace Lokad.Cloud.Services.Runtime.WorkingSet
{
    [Serializable]
    internal sealed class CellArrangement
    {
        public string CellName { get; private set; }
        public CloudServicesSettings ServicesSettings { get; private set; }

        public CellArrangement(
            string cellName,
            IEnumerable<QueuedCloudServiceSettings> queuedCloudServices,
            IEnumerable<ScheduledCloudServiceSettings> scheduledCloudServices,
            IEnumerable<ScheduledWorkerServiceSettings> scheduledWorkerServices,
            IEnumerable<DaemonServiceSettings> daemonServices)
        {
            CellName = cellName;
            ServicesSettings = new CloudServicesSettings
                {
                    QueuedCloudServices = new List<QueuedCloudServiceSettings>(queuedCloudServices),
                    ScheduledCloudServices = new List<ScheduledCloudServiceSettings>(scheduledCloudServices),
                    ScheduledWorkerServices = new List<ScheduledWorkerServiceSettings>(scheduledWorkerServices),
                    DaemonServices = new List<DaemonServiceSettings>(daemonServices)
                };
        }
    }
}
