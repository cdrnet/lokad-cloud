#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;

namespace Lokad.Cloud.Diagnostics
{
    public sealed class LogEntryBuilder
    {
        private readonly IHostLog _log;
        private readonly HostLogLevel _level;
        private readonly List<XElement> _meta;
        private Exception _exception;

        internal LogEntryBuilder(IHostLog log, HostLogLevel level)
        {
            _log = log;
            _level = level;
            _meta = new List<XElement>();
        }

        public LogEntryBuilder WithException(Exception exception)
        {
            _exception = exception;
            return this;
        }

        public LogEntryBuilder WithMeta(XElement element)
        {
            _meta.Add(element);
            return this;
        }

        public LogEntryBuilder WithMeta(params XElement[] elements)
        {
            _meta.AddRange(elements);
            return this;
        }

        public LogEntryBuilder WithMeta(string key, string value)
        {
            _meta.Add(new XElement(key, value));
            return this;
        }

        public void Write(object message)
        {
            _log.Log(_level, _exception, message, _meta.ToArray());
        }

        public void WriteFormat(string format, params object[] args)
        {
            _log.Log(_level, _exception, string.Format(CultureInfo.InvariantCulture, format, args), _meta.ToArray());
        }
    }

    /// <summary>
    /// Helper extensions for any class that implements <see cref="ILog"/>
    /// </summary>
    public static class IHostLogExtensions
    {
        static readonly CultureInfo _culture = CultureInfo.InvariantCulture;

        /// <summary>
        /// Build a log entry incrementally with a builder
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        public static LogEntryBuilder Debug(this IHostLog log)
        {
            return new LogEntryBuilder(log, HostLogLevel.Debug);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Debug"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="message">Message</param>
        /// <param name="meta">Optional semantic meta data</param>
        public static void Debug(this IHostLog log, object message, params XElement[] meta)
        {
            log.Log(HostLogLevel.Debug, message, meta);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Debug"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="format">Format string as in 
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        public static void DebugFormat(this IHostLog log, string format, params object[] args)
        {
            log.Log(HostLogLevel.Debug, string.Format(_culture, format, args));
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Debug"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="format">Format string as in 
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        /// <param name="meta">Optional semantic meta data</param>
        public static void DebugFormat(this IHostLog log, XElement[] meta, string format, params object[] args)
        {
            log.Log(HostLogLevel.Debug, string.Format(_culture, format, args), meta);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Debug"/> level and
        /// appends the specified <see cref="Exception"/>
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="ex">Exception to add to the message</param>
        /// <param name="message">Message</param>
        /// <param name="meta">Optional semantic meta data</param>
        public static void Debug(this IHostLog log, Exception ex, object message, params XElement[] meta)
        {
            log.Log(HostLogLevel.Debug, ex, message, meta);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Debug"/> level and
        /// appends the specified <see cref="Exception"/>
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="ex">Exception to add to the message</param>
        /// <param name="format">Format string as in 
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        public static void DebugFormat(this IHostLog log, Exception ex, string format, params object[] args)
        {
            log.Log(HostLogLevel.Debug, ex, string.Format(_culture, format, args));
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Debug"/> level and
        /// appends the specified <see cref="Exception"/>
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="ex">Exception to add to the message</param>
        /// <param name="format">Format string as in 
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        /// <param name="meta">Optional semantic meta data</param>
        public static void DebugFormat(this IHostLog log, Exception ex, XElement[] meta, string format, params object[] args)
        {
            log.Log(HostLogLevel.Debug, ex, string.Format(_culture, format, args), meta);
        }

        /// <summary>
        /// Build a log entry incrementally with a builder
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        public static LogEntryBuilder Info(this IHostLog log)
        {
            return new LogEntryBuilder(log, HostLogLevel.Info);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Info"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="message">Message</param>
        /// <param name="meta">Optional semantic meta data</param>
        public static void Info(this IHostLog log, object message, params XElement[] meta)
        {
            log.Log(HostLogLevel.Info, message, meta);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Info"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="format">Format string as in 
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        public static void InfoFormat(this IHostLog log, string format, params object[] args)
        {
            log.Log(HostLogLevel.Info, string.Format(_culture, format, args));
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Info"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="format">Format string as in 
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        /// <param name="meta">Optional semantic meta data</param>
        public static void InfoFormat(this IHostLog log, XElement[] meta, string format, params object[] args)
        {
            log.Log(HostLogLevel.Info, string.Format(_culture, format, args), meta);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Info"/> level and
        /// appends the specified <see cref="Exception"/>
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="ex">Exception to add to the message</param>
        /// <param name="message">Message</param>
        /// <param name="meta">Optional semantic meta data</param>
        public static void Info(this IHostLog log, Exception ex, object message, params XElement[] meta)
        {
            log.Log(HostLogLevel.Info, ex, message, meta);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Info"/> level and
        /// appends the specified <see cref="Exception"/>
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="ex">Exception to add to the message</param>
        /// <param name="format">Format string as in 
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        public static void InfoFormat(this IHostLog log, Exception ex, string format, params object[] args)
        {
            log.Log(HostLogLevel.Info, ex, string.Format(_culture, format, args));
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Info"/> level and
        /// appends the specified <see cref="Exception"/>
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="ex">Exception to add to the message</param>
        /// <param name="format">Format string as in 
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        /// <param name="meta">Optional semantic meta data</param>
        public static void InfoFormat(this IHostLog log, Exception ex, XElement[] meta, string format, params object[] args)
        {
            log.Log(HostLogLevel.Info, ex, string.Format(_culture, format, args), meta);
        }

        /// <summary>
        /// Build a log entry incrementally with a builder
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        public static LogEntryBuilder Warn(this IHostLog log)
        {
            return new LogEntryBuilder(log, HostLogLevel.Warn);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Warn"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="message">Message</param>
        /// <param name="meta">Optional semantic meta data</param>
        public static void Warn(this IHostLog log, object message, params XElement[] meta)
        {
            log.Log(HostLogLevel.Warn, message, meta);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Warn"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="format">Format string as in 
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        public static void WarnFormat(this IHostLog log, string format, params object[] args)
        {
            log.Log(HostLogLevel.Warn, string.Format(_culture, format, args));
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Warn"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="format">Format string as in 
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        /// <param name="meta">Optional semantic meta data</param>
        public static void WarnFormat(this IHostLog log, XElement[] meta, string format, params object[] args)
        {
            log.Log(HostLogLevel.Warn, string.Format(_culture, format, args), meta);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Warn"/> level and
        /// appends the specified <see cref="Exception"/>
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="ex">Exception to add to the message</param>
        /// <param name="message">Message</param>
        /// <param name="meta">Optional semantic meta data</param>
        public static void Warn(this IHostLog log, Exception ex, object message, params XElement[] meta)
        {
            log.Log(HostLogLevel.Warn, ex, message, meta);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Warn"/> level and
        /// appends the specified <see cref="Exception"/>
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="ex">Exception to add to the message</param>
        /// <param name="format">Format string as in 
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        public static void WarnFormat(this IHostLog log, Exception ex, string format, params object[] args)
        {
            log.Log(HostLogLevel.Warn, ex, string.Format(_culture, format, args));
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Warn"/> level and
        /// appends the specified <see cref="Exception"/>
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="ex">Exception to add to the message</param>
        /// <param name="format">Format string as in 
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        /// <param name="meta">Optional semantic meta data</param>
        public static void WarnFormat(this IHostLog log, Exception ex, XElement[] meta, string format, params object[] args)
        {
            log.Log(HostLogLevel.Warn, ex, string.Format(_culture, format, args), meta);
        }

        /// <summary>
        /// Build a log entry incrementally with a builder
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        public static LogEntryBuilder Error(this IHostLog log)
        {
            return new LogEntryBuilder(log, HostLogLevel.Error);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Error"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="message">Message</param>
        /// <param name="meta">Optional semantic meta data</param>
        public static void Error(this IHostLog log, object message, params XElement[] meta)
        {
            log.Log(HostLogLevel.Error, message, meta);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Error"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="format">Format string as in 
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        public static void ErrorFormat(this IHostLog log, string format, params object[] args)
        {
            log.Log(HostLogLevel.Error, string.Format(_culture, format, args));
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Error"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="format">Format string as in 
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        /// <param name="meta">Optional semantic meta data</param>
        public static void ErrorFormat(this IHostLog log, XElement[] meta, string format, params object[] args)
        {
            log.Log(HostLogLevel.Error, string.Format(_culture, format, args), meta);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Error"/> level and
        /// appends the specified <see cref="Exception"/>
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="ex">Exception to add to the message</param>
        /// <param name="message">Message</param>
        /// <param name="meta">Optional semantic meta data</param>
        public static void Error(this IHostLog log, Exception ex, object message, params XElement[] meta)
        {
            log.Log(HostLogLevel.Error, ex, message, meta);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Error"/> level and
        /// appends the specified <see cref="Exception"/>
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="ex">Exception to add to the message</param>
        /// <param name="format">Format string as in 
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        public static void ErrorFormat(this IHostLog log, Exception ex, string format, params object[] args)
        {
            log.Log(HostLogLevel.Error, ex, string.Format(_culture, format, args));
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Error"/> level and
        /// appends the specified <see cref="Exception"/>
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="ex">Exception to add to the message</param>
        /// <param name="format">Format string as in 
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        /// <param name="meta">Optional semantic meta data</param>
        public static void ErrorFormat(this IHostLog log, Exception ex, XElement[] meta, string format, params object[] args)
        {
            log.Log(HostLogLevel.Error, ex, string.Format(_culture, format, args), meta);
        }

        /// <summary>
        /// Build a log entry incrementally with a builder
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        public static LogEntryBuilder Fatal(this IHostLog log)
        {
            return new LogEntryBuilder(log, HostLogLevel.Fatal);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Fatal"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="message">Message</param>
        /// <param name="meta">Optional semantic meta data</param>
        public static void Fatal(this IHostLog log, object message, params XElement[] meta)
        {
            log.Log(HostLogLevel.Fatal, message, meta);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Fatal"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="format">Format string as in 
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        public static void FatalFormat(this IHostLog log, string format, params object[] args)
        {
            log.Log(HostLogLevel.Fatal, string.Format(_culture, format, args));
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Fatal"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="format">Format string as in 
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        /// <param name="meta">Optional semantic meta data</param>
        public static void FatalFormat(this IHostLog log, XElement[] meta, string format, params object[] args)
        {
            log.Log(HostLogLevel.Fatal, string.Format(_culture, format, args), meta);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Fatal"/> level and
        /// appends the specified <see cref="Exception"/>
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="ex">Exception to add to the message</param>
        /// <param name="message">Message</param>
        /// <param name="meta">Optional semantic meta data</param>
        public static void Fatal(this IHostLog log, Exception ex, object message, params XElement[] meta)
        {
            log.Log(HostLogLevel.Fatal, ex, message, meta);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Fatal"/> level and
        /// appends the specified <see cref="Exception"/>
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="ex">Exception to add to the message</param>
        /// <param name="format">Format string as in 
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        public static void FatalFormat(this IHostLog log, Exception ex, string format, params object[] args)
        {
            log.Log(HostLogLevel.Fatal, ex, string.Format(_culture, format, args));
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Fatal"/> level and
        /// appends the specified <see cref="Exception"/>
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="ex">Exception to add to the message</param>
        /// <param name="format">Format string as in 
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        /// <param name="meta">Optional semantic meta data</param>
        public static void FatalFormat(this IHostLog log, Exception ex, XElement[] meta, string format, params object[] args)
        {
            log.Log(HostLogLevel.Fatal, ex, string.Format(_culture, format, args), meta);
        }
    }
}
