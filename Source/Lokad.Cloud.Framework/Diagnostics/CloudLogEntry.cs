#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Xml.Linq;

namespace Lokad.Cloud.Diagnostics
{
    /// <summary>
    /// Log entry (when retrieving logs with the <see cref="CloudLogReader"/>).
    /// </summary>
    public class CloudLogEntry
    {
        public DateTime DateTimeUtc { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }
        public string Error { get; set; }
        public XElement Meta { get; set; }
    }
}