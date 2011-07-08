#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Runtime.Serialization;

namespace Lokad.Cloud.Services.Management.Application
{
    // TODO (ruegg, 2011-07-04): Drop legacy service definitions

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
        public QueueServiceDefinition[] QueueServices { get; set; }

        [DataMember]
        public ScheduledServiceDefinition[] ScheduledServices { get; set; }

        [DataMember]
        public CloudServiceDefinition[] CloudServices { get; set; }

        [DataMember]
        public QueuedCloudServiceDefinition[] QueuedCloudServices { get; set; }

        [DataMember]
        public ScheduledCloudServiceDefinition[] ScheduledCloudServices { get; set; }

        [DataMember]
        public ScheduledWorkerServiceDefinition[] ScheduledWorkerServices { get; set; }

        [DataMember]
        public DaemonServiceDefinition[] DaemonServices { get; set; }
    }

    public interface ICloudServiceDefinition
    {
        string TypeName { get; set; }
    }

    [DataContract(Namespace = "http://schemas.lokad.com/lokad-cloud/application/1.1"), Serializable]
    public class QueueServiceDefinition : ICloudServiceDefinition
    {
        [DataMember(IsRequired = true)]
        public string TypeName { get; set; }

        [DataMember(IsRequired = true)]
        public string MessageTypeName { get; set; }

        [DataMember(IsRequired = true)]
        public string QueueName { get; set; }
    }

    [DataContract(Namespace = "http://schemas.lokad.com/lokad-cloud/application/1.1"), Serializable]
    public class ScheduledServiceDefinition : ICloudServiceDefinition
    {
        [DataMember(IsRequired = true)]
        public string TypeName { get; set; }
    }

    [DataContract(Namespace = "http://schemas.lokad.com/lokad-cloud/application/1.1"), Serializable]
    public class CloudServiceDefinition : ICloudServiceDefinition
    {
        [DataMember(IsRequired = true)]
        public string TypeName { get; set; }
    }

    [DataContract(Namespace = "http://schemas.lokad.com/lokad-cloud/application/1.1"), Serializable]
    public class QueuedCloudServiceDefinition : ICloudServiceDefinition
    {
        [DataMember(IsRequired = true)]
        public string TypeName { get; set; }

        [DataMember(IsRequired = true)]
        public string MessageTypeName { get; set; }

        [DataMember(IsRequired = true)]
        public string QueueName { get; set; }
    }

    [DataContract(Namespace = "http://schemas.lokad.com/lokad-cloud/application/1.1"), Serializable]
    public class ScheduledCloudServiceDefinition : ICloudServiceDefinition
    {
        [DataMember(IsRequired = true)]
        public string TypeName { get; set; }
    }

    [DataContract(Namespace = "http://schemas.lokad.com/lokad-cloud/application/1.1"), Serializable]
    public class ScheduledWorkerServiceDefinition : ICloudServiceDefinition
    {
        [DataMember(IsRequired = true)]
        public string TypeName { get; set; }
    }

    [DataContract(Namespace = "http://schemas.lokad.com/lokad-cloud/application/1.1"), Serializable]
    public class DaemonServiceDefinition : ICloudServiceDefinition
    {
        [DataMember(IsRequired = true)]
        public string TypeName { get; set; }
    }
}
