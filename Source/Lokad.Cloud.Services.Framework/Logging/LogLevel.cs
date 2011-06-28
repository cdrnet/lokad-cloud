#region (c)2009-2011 Lokad - New BSD license
// Company: http://www.lokad.com
// This code is released under the terms of the new BSD licence
#endregion

using System.Runtime.Serialization;

namespace Lokad.Cloud.Services.Framework.Logging
{
    [DataContract(Namespace = "http://schemas.lokad.com/lokad-cloud/services/logging/1.0")]
    public enum LogLevel
    {
        /// <summary> Message is intended for debugging </summary>
        [EnumMember]
        Debug,
        
        /// <summary> Informatory message </summary>
        [EnumMember]
        Info,
        
        /// <summary> The message is about potential problem in the system </summary>
        [EnumMember]
        Warn,
        
        /// <summary> Some error has occured </summary>
        [EnumMember]
        Error,
        
        /// <summary> Message is associated with the critical problem </summary>
        [EnumMember]
        Fatal,

        /// <summary>
        /// Highest possible level
        /// </summary>
        Max = int.MaxValue,
        /// <summary> Smallest logging level</summary>
        Min = int.MinValue
    }
}
