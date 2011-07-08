﻿using System;
using System.Runtime.Serialization;

namespace Lokad.Cloud.Services.Runtime.Settings
{
    [DataContract(Namespace = "http://schemas.lokad.com/lokad-cloud/services/settings/1.0"), Serializable]
    internal class ScheduledWorkerServiceSettings : CommonServiceSettings, IExtensibleDataObject
    {
        [DataMember]
        public TimeSpan ProcessingTimeout { get; set; }

        [DataMember]
        public TimeSpan TriggerInterval { get; set; }

        [NonSerialized]
        private ExtensionDataObject _extensionData;
        ExtensionDataObject IExtensibleDataObject.ExtensionData
        {
            get { return _extensionData; }
            set { _extensionData = value; }
        }
    }
}