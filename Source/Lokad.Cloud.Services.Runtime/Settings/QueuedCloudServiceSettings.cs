using System;
using System.Runtime.Serialization;

namespace Lokad.Cloud.Services.Runtime.Settings
{
    [DataContract(Namespace = "http://schemas.lokad.com/lokad-cloud/services/settings/1.0"), Serializable]
    internal class QueuedCloudServiceSettings : CommonServiceSettings, IExtensibleDataObject
    {
        [DataMember]
        public TimeSpan ProcessingTimeout { get; set; }

        [DataMember]
        public string MessageTypeName { get; set; }

        [DataMember]
        public string QueueName { get; set; }

        [DataMember]
        public TimeSpan VisibilityTimeout { get; set; }

        [DataMember]
        public TimeSpan ContinueProcessingIfMessagesAvailable { get; set; }

        [DataMember]
        public int MaxProcessingTrials { get; set; }

        [NonSerialized]
        private ExtensionDataObject _extensionData;
        ExtensionDataObject IExtensibleDataObject.ExtensionData
        {
            get { return _extensionData; }
            set { _extensionData = value; }
        }
    }
}