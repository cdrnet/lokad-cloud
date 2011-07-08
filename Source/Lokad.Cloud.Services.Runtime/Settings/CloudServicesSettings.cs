#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Lokad.Cloud.Services.Runtime.Settings
{
    /// <summary>
    /// Runtime-relevant settings for all cloud services
    /// </summary>
    [DataContract(Namespace = "http://schemas.lokad.com/lokad-cloud/services/settings/1.0"), Serializable]
    internal class CloudServicesSettings : IExtensibleDataObject
    {
        [DataMember]
        public List<QueuedCloudServiceSettings> QueuedCloudServices { get; set; }

        [DataMember]
        public List<ScheduledCloudServiceSettings> ScheduledCloudServices { get; set; }

        [DataMember]
        public List<ScheduledWorkerServiceSettings> ScheduledWorkerServices { get; set; }

        [DataMember]
        public List<DaemonServiceSettings> DaemonServices { get; set; }

        [NonSerialized]
        private ExtensionDataObject _extensionData;
        ExtensionDataObject IExtensibleDataObject.ExtensionData
        {
            get { return _extensionData; }
            set { _extensionData = value; }
        }
    }
}
