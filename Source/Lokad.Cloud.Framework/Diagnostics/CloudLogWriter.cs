#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Globalization;
using System.Xml.Linq;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.Diagnostics
{
    /// <summary>
    /// Logger built on top of the Blob Storage.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Logs are formatted in XML with
    /// <code>
    /// &lt;log&gt;
    ///   &lt;message&gt; {0} &lt;/message&gt;
    ///   &lt;error&gt; {1} &lt;/error&gt;
    /// &lt;/log&gt;
    /// </code>
    /// Also, the logger is relying on date prefix in order to facilitate large
    /// scale enumeration of the logs. Yet, in order to facilitate fast enumeration
    /// of recent logs, a prefix inversion trick is used.
    /// </para>
    /// <para>
    /// We put entries to different containers depending on the log level. This helps
    /// reading only interesting entries and easily skipping those below the threshold.
    /// An entry is put to one container matching the level only, not to all containers
    /// with the matching or a lower level. This is a trade off to avoid optimizing
    /// for read speed at the cost of write speed, because we assume more frequent
    /// writes than reads and, more importantly, writes to happen in time-critical
    /// code paths while reading is almost never time-critical.
    /// </para>
    /// </remarks>
    public class CloudLogWriter : ILog
    {
        private const string ContainerNamePrefix = "lokad-cloud-logs";

        private readonly IBlobStorageProvider _blobStorage;

        public CloudLogWriter(IBlobStorageProvider blobStorage)
        {
            _blobStorage = blobStorage;
        }

        public void Log(LogLevel level, object message, params XElement[] meta)
        {
            Log(level, null, message, meta);
        }

        public void Log(LogLevel level, Exception exception, object message, params XElement[] meta)
        {
            var now = DateTime.UtcNow;

            var blobContent = FormatLogEntry(now, level, message.ToString(), exception, meta);
            var blobName = string.Format("{0}/{1}/", FormatDateTimeNamePrefix(now), level);
            var blobContainer = LevelToContainer(level);

            var attempt = 0;
            while (!_blobStorage.PutBlob(blobContainer, blobName + attempt, blobContent, false))
            {
                attempt++;
            }
        }

        private static string LevelToContainer(LogLevel level)
        {
            return ContainerNamePrefix + "-" + level.ToString().ToLower();
        }

        private static string FormatLogEntry(DateTime dateTimeUtc, LogLevel level, string message, Exception exception, XElement[] meta)
        {
            var entry = new XElement("log",
                new XElement("level", level),
                new XElement("timestamp", dateTimeUtc.ToString("o", CultureInfo.InvariantCulture)),
                new XElement("message", message));

            if (exception != null)
            {
                entry.Add(new XElement("error", exception.ToString()));
            }

            if (meta != null && meta.Length > 0)
            {
                entry.Add(new XElement("Meta", meta));
            }

            return entry.ToString();
        }

        /// <summary>Time prefix with inversion in order to enumerate
        /// starting from the most recent.</summary>
        /// <remarks>This method is the symmetric of <see cref="CloudLogReader.ParseDateTimeFromName"/>.</remarks>
        public static string FormatDateTimeNamePrefix(DateTime dateTimeUtc)
        {
            // yyyy/MM/dd/hh/mm/ss/fff
            return string.Format("{0}/{1}/{2}/{3}/{4}/{5}/{6}",
                (10000 - dateTimeUtc.Year).ToString(CultureInfo.InvariantCulture),
                (12 - dateTimeUtc.Month).ToString("00"),
                (31 - dateTimeUtc.Day).ToString("00"),
                (24 - dateTimeUtc.Hour).ToString("00"),
                (60 - dateTimeUtc.Minute).ToString("00"),
                (60 - dateTimeUtc.Second).ToString("00"),
                (999 - dateTimeUtc.Millisecond).ToString("000"));
        }
    }
}