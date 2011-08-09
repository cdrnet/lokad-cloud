#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Runtime.Serialization;

namespace Lokad.Cloud.Services.Management.Application
{
    [DataContract(Namespace = "http://schemas.lokad.com/lokad-cloud/application/1.1"), Serializable]
    public class CloudApplicationDefinition
    {
        [DataMember(IsRequired = true)]
        public string PackageETag { get; set; }

        [DataMember(IsRequired = true)]
        public DateTimeOffset Timestamp { get; set; }

        [DataMember]
        public CloudApplicationAssemblyInfo[] Assemblies { get; set; }

        [DataMember]
        public DaemonServiceDefinition[] DaemonServices { get; set; }

        [DataMember]
        public QueuedCloudServiceDefinition[] QueuedCloudServices { get; set; }

        [DataMember]
        public ScheduledCloudServiceDefinition[] ScheduledCloudServices { get; set; }

        [DataMember]
        public ScheduledWorkerServiceDefinition[] ScheduledWorkerServices { get; set; }
    }

    [DataContract(Namespace = "http://schemas.lokad.com/lokad-cloud/application/1.1"), Serializable]
    public class DaemonServiceDefinition
    {
        [DataMember(IsRequired = true)]
        public string TypeName { get; set; }
    }

    [DataContract(Namespace = "http://schemas.lokad.com/lokad-cloud/application/1.1"), Serializable]
    public class QueuedCloudServiceDefinition
    {
        [DataMember(IsRequired = true)]
        public string TypeName { get; set; }

        [DataMember(IsRequired = true)]
        public string MessageTypeName { get; set; }

        [DataMember(IsRequired = true)]
        public string QueueName { get; set; }
    }

    [DataContract(Namespace = "http://schemas.lokad.com/lokad-cloud/application/1.1"), Serializable]
    public class ScheduledCloudServiceDefinition
    {
        [DataMember(IsRequired = true)]
        public string TypeName { get; set; }
    }

    [DataContract(Namespace = "http://schemas.lokad.com/lokad-cloud/application/1.1"), Serializable]
    public class ScheduledWorkerServiceDefinition
    {
        [DataMember(IsRequired = true)]
        public string TypeName { get; set; }
    }
}
