#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Autofac;
using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.Management;
using Lokad.Cloud.Storage;
using NUnit.Framework;

namespace Lokad.Cloud.Test.Diagnostics
{
    [TestFixture]
    public class CloudLoggerTests
    {
        readonly CloudLogWriter _logWriter = (CloudLogWriter)GlobalSetup.Container.Resolve<ILog>();
        readonly CloudLogReader _logReader = new CloudLogReader(GlobalSetup.Container.Resolve<CloudStorageProviders>().BlobStorage);

        [SetUp]
        public void Setup()
        {
            // cleanup
            _logReader.DeleteOldLogs(DateTime.UtcNow.AddDays(1));
        }

        [Test]
        public void CanWriteToLog()
        {
            Assert.IsTrue(_logWriter.TryError("My message with CloudLoggerTests.Log.", new InvalidOperationException("CloudLoggerTests.Log")));
            Assert.IsTrue(_logWriter.TryInfo("Not a restart, just a test.", new InvalidOperationException("CloudLoggerTests.Log")));

            _logWriter.Warn().WithException(new Exception())
                .WithMeta("JobId", "123").WithMeta(new XElement("abc", "def"))
                .WriteFormat("Just a {0} Test", "simple");
        }

        [Test]
        public void CanGetLogsOfLevel()
        {
            Thread.Sleep(100);
            var now = DateTime.UtcNow;
            Thread.Sleep(250);

            Assert.IsTrue(_logWriter.TryError("My message with CloudLoggerTests.Log.", new InvalidOperationException("CloudLoggerTests.Log")));
            Assert.IsTrue(_logWriter.TryInfo("Not a restart, just a test.", new InvalidOperationException("CloudLoggerTests.Log")));
            Assert.IsTrue(_logWriter.TryInfo("Not a restart, just a test II.", new InvalidOperationException("CloudLoggerTests.Log")));

            Assert.AreEqual(0, _logReader.GetLogsOfLevel(LogLevel.Fatal).Count(l => l.DateTimeUtc > now));
            Assert.AreEqual(1, _logReader.GetLogsOfLevel(LogLevel.Error).Count(l => l.DateTimeUtc > now));
            Assert.AreEqual(0, _logReader.GetLogsOfLevel(LogLevel.Warn).Count(l => l.DateTimeUtc > now));
            Assert.AreEqual(2, _logReader.GetLogsOfLevel(LogLevel.Info).Count(l => l.DateTimeUtc > now));
            Assert.AreEqual(0, _logReader.GetLogsOfLevel(LogLevel.Debug).Count(l => l.DateTimeUtc > now));
        }

        [Test]
        public void CanGetLogsOfLevelOrHigher()
        {
            Thread.Sleep(100);
            var now = DateTime.UtcNow;
            Thread.Sleep(250);

            Assert.IsTrue(_logWriter.TryError("My message with CloudLoggerTests.Log.", new InvalidOperationException("CloudLoggerTests.Log")));
            Assert.IsTrue(_logWriter.TryInfo("Not a restart, just a test.", new InvalidOperationException("CloudLoggerTests.Log")));
            Assert.IsTrue(_logWriter.TryInfo("Not a restart, just a test II.", new InvalidOperationException("CloudLoggerTests.Log")));

            Assert.AreEqual(0, _logReader.GetLogsOfLevelOrHigher(LogLevel.Fatal).Count(l => l.DateTimeUtc > now));
            Assert.AreEqual(1, _logReader.GetLogsOfLevelOrHigher(LogLevel.Error).Count(l => l.DateTimeUtc > now));
            Assert.AreEqual(1, _logReader.GetLogsOfLevelOrHigher(LogLevel.Warn).Count(l => l.DateTimeUtc > now));
            Assert.AreEqual(3, _logReader.GetLogsOfLevelOrHigher(LogLevel.Info).Count(l => l.DateTimeUtc > now));
            Assert.AreEqual(3, _logReader.GetLogsOfLevelOrHigher(LogLevel.Debug).Count(l => l.DateTimeUtc > now));
        }

        [Test]
        public void GetPagedLogs()
        {
            Thread.Sleep(100);
            var now = DateTime.UtcNow;
            Thread.Sleep(250);

            // Add 30 log messages
            for (int i = 0; i < 10; i++)
            {
                Assert.IsTrue(_logWriter.TryError("My message with CloudLoggerTests.Log.", new InvalidOperationException("CloudLoggerTests.Log")));
                Assert.IsTrue(_logWriter.TryWarn("A test warning"));
                Assert.IsTrue(_logWriter.TryInfo("Not a restart, just a test.", new InvalidOperationException("CloudLoggerTests.Log")));
            }

            Assert.AreEqual(10, _logReader.GetLogs().Take(10).Count());
            Assert.AreEqual(10, _logReader.GetLogs(10).Take(10).Count());
            Assert.AreEqual(10, _logReader.GetLogs(20).Take(10).Count());
            Assert.IsTrue(_logReader.GetLogs(22).Where(l => l.DateTimeUtc > now).Take(22).Count() >= 8);
            Assert.IsTrue(_logReader.GetLogs(25).Where(l => l.DateTimeUtc > now).Take(25).Count() >= 5);
            Assert.AreEqual(0, _logReader.GetLogs(100000).Where(l => l.DateTimeUtc > now).Take(20).Count());
        }

        [Test]
        public void LogsAreReturnedInCorrectOrder()
        {
            var reference = DateTime.UtcNow;
            Thread.Sleep(100);
            var before = DateTime.UtcNow;
            Thread.Sleep(100);
            Assert.IsTrue(_logWriter.TryError("Not a restart, just a test.", new InvalidOperationException("CloudLoggerTests.Log")));
            Thread.Sleep(100);
            Assert.IsTrue(_logWriter.TryInfo("Not a restart, just a test II.", new InvalidOperationException("CloudLoggerTests.Log")));
            Thread.Sleep(100);
            Assert.IsTrue(_logWriter.TryWarn("Not a restart, just a test III.", new InvalidOperationException("CloudLoggerTests.Log")));
            Thread.Sleep(100);
            var after = DateTime.UtcNow;

            var entries = _logReader.GetLogsOfLevelOrHigher(LogLevel.Info).Take(3).ToList();

            var beforeRef = (before - reference).TotalMilliseconds;
            var afterRef = (after - reference).TotalMilliseconds;
            var entry0Ref = (entries[0].DateTimeUtc - reference).TotalMilliseconds;
            var entry1Ref = (entries[1].DateTimeUtc - reference).TotalMilliseconds;
            var entry2Ref = (entries[2].DateTimeUtc - reference).TotalMilliseconds;

            Assert.IsTrue(entry0Ref > beforeRef && entry0Ref < afterRef);
            Assert.IsTrue(entry1Ref > beforeRef && entry1Ref < afterRef);
            Assert.IsTrue(entry2Ref > beforeRef && entry2Ref < afterRef);
            Assert.IsTrue(entry0Ref > entry1Ref);
            Assert.IsTrue(entry1Ref > entry2Ref);
        }

        [Test]
        public void DeleteOldLogs()
        {
            int initialCount = _logReader.GetLogs().Count();

            var begin = DateTime.Now;
            Thread.Sleep(1000); // Wait to make sure that logs are created after 'begin'

            for (int i = 0; i < 10; i++)
            {
                Assert.IsTrue(_logWriter.TryInfo("Just a test message"));
            }

            Assert.AreEqual(initialCount + 10, _logReader.GetLogs().Count());

            _logReader.DeleteOldLogs(begin.ToUniversalTime());

            Assert.AreEqual(10, _logReader.GetLogs().Count());
        }

        [Test]
        public void ToPrefixToDateTime()
        {
            var now = DateTime.UtcNow;

            var rounded = new DateTime(
                now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, now.Millisecond, DateTimeKind.Utc);

            var prefix = CloudLogWriter.FormatDateTimeNamePrefix(rounded);
            var roundedBis = CloudLogReader.ParseDateTimeFromName(prefix);

            Assert.AreEqual(rounded, roundedBis, "#A00");
        }
    }
}
