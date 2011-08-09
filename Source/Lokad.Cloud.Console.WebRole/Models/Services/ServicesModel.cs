#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.Services.Management;
using Lokad.Cloud.Services.Management.Application;

namespace Lokad.Cloud.Console.WebRole.Models.Services
{
    public class ServicesModel
    {
        public QueueServiceModel[] QueuedCloudServices { get; set; }
        public CloudServiceInfo[] ScheduledCloudServices { get; set; }
        public CloudServiceInfo[] ScheduledWorkerServices { get; set; }
        public CloudServiceInfo[] DaemonServices { get; set; }
    }

    public class QueueServiceModel
    {
        public string ServiceName { get; set; }
        public bool IsStarted { get; set; }
        public QueuedCloudServiceDefinition Definition { get; set; }
    }
}