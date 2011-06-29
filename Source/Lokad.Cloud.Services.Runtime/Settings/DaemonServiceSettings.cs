using System.Runtime.Serialization;

namespace Lokad.Cloud.Services.Runtime.Settings
{
    [DataContract(Namespace = "http://schemas.lokad.com/lokad-cloud/services/settings/1.0")]
    internal class DaemonServiceSettings : CommonServiceSettings, IExtensibleDataObject
    {
        public ExtensionDataObject ExtensionData { get; set; }
    }
}