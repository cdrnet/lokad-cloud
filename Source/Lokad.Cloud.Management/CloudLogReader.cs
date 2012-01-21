#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.Management
{
    public class CloudLogReader
    {
        private const string ContainerNamePrefix = "lokad-cloud-logs";
        private const int DeleteBatchSize = 50;

        private readonly IBlobStorageProvider _blobs;
        private readonly IDataSerializer _runtimeFormatter;

        public CloudLogReader(IBlobStorageProvider blobStorage)
        {
            _blobs = blobStorage;
            _runtimeFormatter = new CloudFormatter();
        }

        /// <summary>
        /// Lazily enumerate all logs of the specified level, ordered with the newest entry first.
        /// </summary>
        public IEnumerable<CloudLogEntry> GetLogsOfLevel(LogLevel level, int skip = 0)
        {
            return _blobs
                .ListBlobs<string>(LevelToContainer(level), skip: skip, serializer: _runtimeFormatter)
                .Select(ParseLogEntry);
        }

        /// <summary>
        /// Lazily enumerate all logs of the specified level or higher, ordered with the newest entry first.
        /// </summary>
        public IEnumerable<CloudLogEntry> GetLogsOfLevelOrHigher(LogLevel levelThreshold, int skip = 0)
        {
            // We need to sort by date (desc), but want to do it lazily based on
            // the guarantee that the enumerators themselves are ordered alike.
            // To do that we always select the newest value, move next, and repeat.

            var enumerators = Enum.GetValues(typeof(LogLevel)).OfType<LogLevel>()
                .Where(l => l >= levelThreshold && l < LogLevel.Max && l > LogLevel.Min)
                .Select(level =>
                    {
                        var containerName = LevelToContainer(level);
                        return _blobs.ListBlobNames(containerName, string.Empty)
                            .Select(blobName => Tuple.Create(containerName, blobName))
                            .GetEnumerator();
                    })
                .ToList();

            for (var i = enumerators.Count - 1; i >= 0; i--)
            {
                if (!enumerators[i].MoveNext())
                {
                    enumerators.RemoveAt(i);
                }
            }

            // Skip
            for (var i = skip; i > 0 && enumerators.Count > 0; i--)
            {
                var max = enumerators.Aggregate((left, right) => String.CompareOrdinal(left.Current.Item2, right.Current.Item2) < 0 ? left : right);
                if (!max.MoveNext())
                {
                    enumerators.Remove(max);
                }
            }

            // actual iterator
            while (enumerators.Count > 0)
            {
                var max = enumerators.Aggregate((left, right) => String.CompareOrdinal(left.Current.Item2, right.Current.Item2) < 0 ? left : right);
                var blob = _blobs.GetBlob<string>(max.Current.Item1, max.Current.Item2, _runtimeFormatter);
                if (blob.HasValue)
                {
                    yield return ParseLogEntry(blob.Value);
                }

                if (!max.MoveNext())
                {
                    enumerators.Remove(max);
                }
            }
        }

        /// <summary>Lazily enumerates over the entire logs.</summary>
        /// <returns></returns>
        public IEnumerable<CloudLogEntry> GetLogs(int skip = 0)
        {
            return GetLogsOfLevelOrHigher(LogLevel.Min, skip);
        }

        /// <summary>
        /// Deletes all logs of all levels.
        /// </summary>
        public void DeleteAllLogs()
        {
            foreach (var level in Enum.GetValues(typeof(LogLevel)).OfType<LogLevel>()
                .Where(l => l < LogLevel.Max && l > LogLevel.Min))
            {
                _blobs.DeleteContainerIfExist(LevelToContainer(level));
            }
        }

        /// <summary>
        /// Deletes all the logs older than the provided date.
        /// </summary>
        public void DeleteOldLogs(DateTime olderThanUtc)
        {
            foreach (var level in Enum.GetValues(typeof(LogLevel)).OfType<LogLevel>()
                .Where(l => l < LogLevel.Max && l > LogLevel.Min))
            {
                DeleteOldLogsOfLevel(level, olderThanUtc);
            }
        }

        /// <summary>
        /// Deletes all the logs of a level and older than the provided date.
        /// </summary>
        public void DeleteOldLogsOfLevel(LogLevel level, DateTime olderThanUtc)
        {
            // Algorithm:
            // Iterate over the logs, queuing deletions up to 50 items at a time,
            // then restart; continue until no deletions are queued

            var deleteQueue = new List<string>(DeleteBatchSize);
            var blobContainer = LevelToContainer(level);

            do
            {
                deleteQueue.Clear();

                foreach (var blobName in _blobs.ListBlobNames(blobContainer, string.Empty))
                {
                    var dateTime = ParseDateTimeFromName(blobName);
                    if (dateTime < olderThanUtc) deleteQueue.Add(blobName);

                    if (deleteQueue.Count == DeleteBatchSize) break;
                }

                foreach (var blobName in deleteQueue)
                {
                    _blobs.DeleteBlobIfExist(blobContainer, blobName);
                }

            } while (deleteQueue.Count > 0);
        } 

        private static string LevelToContainer(LogLevel level)
        {
            return ContainerNamePrefix + "-" + level.ToString().ToLower();
        }

        private static CloudLogEntry ParseLogEntry(string blobContent)
        {
            var xml = XElement.Parse(blobContent);
            return new CloudLogEntry
                {
                    Level = xml.Element("level").ValueOrDefault(),
                    DateTimeUtc = xml.Element("timestamp").ProjectOrDefault(x => DateTime.ParseExact(x.Value, "o", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToUniversalTime()),
                    Message = xml.Element("message").ValueOrEmpty(),
                    Error = xml.Element("error").ValueOrEmpty(),
                    Meta = xml.Element("meta").ProjectOrDefault(x => x.Elements().ToArray(), () => new XElement[0])
                };
        }

        /// <summary>Convert a prefix with inversion into a <c>DateTime</c>.</summary>
        /// <remarks>This method is the symmetric of <see cref="CloudLogWriter.FormatDateTimeNamePrefix"/>.</remarks>
        public static DateTime ParseDateTimeFromName(string nameOrPrefix)
        {
            // prefix is always 23 char long
            var tokens = nameOrPrefix.Substring(0, 23).Split('/');

            if (tokens.Length != 7) throw new ArgumentException("Incorrect prefix.", "nameOrPrefix");

            var year = 10000 - int.Parse(tokens[0], CultureInfo.InvariantCulture);
            var month = 12 - int.Parse(tokens[1], CultureInfo.InvariantCulture);
            var day = 31 - int.Parse(tokens[2], CultureInfo.InvariantCulture);
            var hour = 24 - int.Parse(tokens[3], CultureInfo.InvariantCulture);
            var minute = 60 - int.Parse(tokens[4], CultureInfo.InvariantCulture);
            var second = 60 - int.Parse(tokens[5], CultureInfo.InvariantCulture);
            var millisecond = 999 - int.Parse(tokens[6], CultureInfo.InvariantCulture);

            return new DateTime(year, month, day, hour, minute, second, millisecond, DateTimeKind.Utc);
        }
    }
}
