using System;

namespace Lokad.Cloud.Services.Management.Logging
{
    public class CloudLogEntry
    {
        public DateTime DateTimeUtc { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }
        public string Error { get; set; }
        public string Source { get; set; }
    }
}