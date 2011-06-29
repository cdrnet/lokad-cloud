#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;

namespace Lokad.Cloud.Services.Management.Logging
{
    public class CloudLogEntry
    {
        public DateTime DateTimeUtc { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }
        public string Error { get; set; }
    }
}