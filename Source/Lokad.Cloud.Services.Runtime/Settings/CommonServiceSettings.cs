using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Lokad.Cloud.Services.Runtime.Settings
{
    /// <summary>
    /// Common settings shared by all cloud services.
    /// </summary>
    [DataContract(Namespace = "http://schemas.lokad.com/lokad-cloud/services/settings/1.0"), Serializable]
    [KnownType(typeof(QueuedCloudServiceSettings)), KnownType(typeof(ScheduledCloudServiceSettings)), KnownType(typeof(ScheduledWorkerServiceSettings)), KnownType(typeof(DaemonServiceSettings))]
    internal abstract class CommonServiceSettings
    {
        /// <summary>
        /// Full type name of the service, used for IoC activation.
        /// </summary>
        [DataMember(IsRequired = true)]
        public string TypeName { get; set; }

        /// <summary>
        /// True if the service should be ignored.
        /// </summary>
        [DataMember]
        public bool IsDisabled { get; set; }

        /// <summary>
        /// List of runtime cells this service should execute on.
        /// No entry is interpreted as the 'default' cell only.
        /// </summary>
        [DataMember]
        public List<string> CellAffinity { get; set; }
    }
}