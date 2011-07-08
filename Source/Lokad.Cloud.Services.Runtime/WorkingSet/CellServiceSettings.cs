using System;
using Lokad.Cloud.Services.Management.Settings;

namespace Lokad.Cloud.Services.Runtime.WorkingSet
{
    [Serializable]
    internal sealed class CellServiceSettings
    {
        public string CellName { get; private set; }
        public QueuedCloudServiceSettings[] QueuedCloudServices { get; private set; }
        public ScheduledCloudServiceSettings[] ScheduledCloudServices { get; private set; }
        public ScheduledWorkerServiceSettings[] ScheduledWorkerServices { get; private set; }
        public DaemonServiceSettings[] DaemonServices { get; private set; }

        public CellServiceSettings(
            string cellName,
            QueuedCloudServiceSettings[] queuedCloudServices,
            ScheduledCloudServiceSettings[] scheduledCloudServices,
            ScheduledWorkerServiceSettings[] scheduledWorkerServices,
            DaemonServiceSettings[] daemonServices)
        {
            CellName = cellName;
            QueuedCloudServices = queuedCloudServices;
            ScheduledCloudServices = scheduledCloudServices;
            ScheduledWorkerServices = scheduledWorkerServices;
            DaemonServices = daemonServices;
        }

    }
}
