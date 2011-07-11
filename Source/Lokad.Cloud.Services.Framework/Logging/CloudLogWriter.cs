using System;
using System.Globalization;
using System.Security;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.Services.Framework.Logging
{
    public class CloudLogWriter : ILogWriter
    {
        private const string ContainerNamePrefix = "lokad-cloud-logs";

        private readonly IBlobStorageProvider _blobStorage;

        public CloudLogWriter(CloudStorageProviders storage)
        {
            _blobStorage = storage.NeutralBlobStorage;
        }

        public void Log(LogLevel level, object message)
        {
            Log(level, null, message);
        }

        public void Log(LogLevel level, Exception ex, object message)
        {
            var now = DateTime.UtcNow;

            var blobContent = FormatLogEntry(now, level, message.ToString(), ex != null ? ex.ToString() : string.Empty);
            var blobName = string.Format("{0}/{1}/", FormatDateTimeNamePrefix(now), level);
            var blobContainer = LevelToContainer(level);

            var attempt = 0;
            while (!_blobStorage.PutBlob(blobContainer, blobName + attempt, blobContent, false))
            {
                attempt++;
            }
        }

        private static string FormatLogEntry(DateTime dateTimeUtc, LogLevel level, string message, string error)
        {
            // TODO: drop empty source tag
            return string.Format(
                @"
<log>
  <level>{0}</level>
  <timestamp>{1}</timestamp>
  <message>{2}</message>
  <error>{3}</error>
  <source></source>
</log>
",
                level,
                dateTimeUtc.ToString("o", CultureInfo.InvariantCulture),
                SecurityElement.Escape(message),
                SecurityElement.Escape(error));
        }

        private static string LevelToContainer(LogLevel level)
        {
            return ContainerNamePrefix + "-" + level.ToString().ToLower();
        }

        /// <summary>Time prefix with inversion in order to enumerate
        /// starting from the most recent.</summary>
        /// <remarks>This method is the symmetric of <see cref="ParseDateTimeFromName"/>.</remarks>
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
