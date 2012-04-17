#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Globalization;
using System.Xml.Linq;
using Lokad.Cloud.Annotations;

namespace Lokad.Cloud.Diagnostics
{
    /// <summary>
    /// Helper extensions for any class that implements <see cref="ILog"/>
    /// </summary>
    public static class ILogExtensions
    {
        static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

        /// <summary>
        /// Tries to write a log entry.
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="level">The importance level</param>
        /// <param name="message">The actual message</param>
        /// <param name="exception">The actual exception</param>
        /// <param name="meta">Optional semantic meta data</param>
        public static bool TryLog(this ILog log, LogLevel level, string message, Exception exception = null, XElement meta = null)
        {
            try
            {
                log.Log(level, message, exception, meta);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Tries to write a log entry.
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="level">The importance level</param>
        /// <param name="format">Format string as in
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        [StringFormatMethod("format")]
        public static bool TryLogFormat(this ILog log, LogLevel level, string format, params object[] args)
        {
            return log.TryLog(level, string.Format(Culture, format, args));
        }


        /// <summary>
        /// Tries to write a log entry.
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="level">The importance level</param>
        /// <param name="exception">The actual exception</param>
        /// <param name="format">Format string as in
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        [StringFormatMethod("format")]
        public static bool TryLogFormat(this ILog log, LogLevel level, Exception exception, string format, params object[] args)
        {
            return log.TryLog(level, string.Format(Culture, format, args), exception);
        }

        /// <summary>
        /// Build a log entry incrementally with a builder
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        public static LogEntryBuilder Debug(this ILog log)
        {
            return new LogEntryBuilder(log, LogLevel.Debug);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Debug"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="message">Message</param>
        /// <param name="exception">Exception to add to the message</param>
        /// <param name="meta">Optional semantic meta data</param>
        public static bool TryDebug(this ILog log, string message, Exception exception = null, XElement meta = null)
        {
            return log.TryLog(LogLevel.Debug, message, exception, meta);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Debug"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="format">Format string as in
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        [StringFormatMethod("format")]
        public static bool TryDebugFormat(this ILog log, string format, params object[] args)
        {
            return log.TryLogFormat(LogLevel.Debug, format, args);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Debug"/> level and
        /// appends the specified <see cref="Exception"/>
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="exception">Exception to add to the message</param>
        /// <param name="format">Format string as in
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        [StringFormatMethod("format")]
        public static bool TryDebugFormat(this ILog log, Exception exception, string format, params object[] args)
        {
            return log.TryLogFormat(LogLevel.Debug, exception, format, args);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Debug"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="message">Message</param>
        /// <param name="meta">Optional semantic meta data</param>
        [Obsolete("Will be dropped in the next release")]
        public static void Debug(this ILog log, object message, params XElement[] meta)
        {
            log.Log(LogLevel.Debug, message, meta);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Debug"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="format">Format string as in 
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        [StringFormatMethod("format")]
        [Obsolete("Will be dropped in the next release")]
        public static void DebugFormat(this ILog log, string format, params object[] args)
        {
            log.Log(LogLevel.Debug, string.Format(Culture, format, args));
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Debug"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="format">Format string as in 
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        /// <param name="meta">Optional semantic meta data</param>
        [StringFormatMethod("format")]
        [Obsolete("Will be dropped in the next release")]
        public static void DebugFormat(this ILog log, XElement[] meta, string format, params object[] args)
        {
            log.Log(LogLevel.Debug, string.Format(Culture, format, args), meta);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Debug"/> level and
        /// appends the specified <see cref="Exception"/>
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="ex">Exception to add to the message</param>
        /// <param name="message">Message</param>
        /// <param name="meta">Optional semantic meta data</param>
        [Obsolete("Will be dropped in the next release")]
        public static void Debug(this ILog log, Exception ex, object message, params XElement[] meta)
        {
            log.Log(LogLevel.Debug, ex, message, meta);
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
        [StringFormatMethod("format")]
        [Obsolete("Will be dropped in the next release")]
        public static void DebugFormat(this ILog log, Exception ex, string format, params object[] args)
        {
            log.Log(LogLevel.Debug, ex, string.Format(Culture, format, args));
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
        [StringFormatMethod("format")]
        [Obsolete("Will be dropped in the next release")]
        public static void DebugFormat(this ILog log, Exception ex, XElement[] meta, string format, params object[] args)
        {
            log.Log(LogLevel.Debug, ex, string.Format(Culture, format, args), meta);
        }

        /// <summary>
        /// Build a log entry incrementally with a builder
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        public static LogEntryBuilder Info(this ILog log)
        {
            return new LogEntryBuilder(log, LogLevel.Info);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Info"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="message">Message</param>
        /// <param name="exception">Exception to add to the message</param>
        /// <param name="meta">Optional semantic meta data</param>
        public static bool TryInfo(this ILog log, string message, Exception exception = null, XElement meta = null)
        {
            return log.TryLog(LogLevel.Info, message, exception, meta);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Info"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="format">Format string as in
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        [StringFormatMethod("format")]
        public static bool TryInfoFormat(this ILog log, string format, params object[] args)
        {
            return log.TryLogFormat(LogLevel.Info, format, args);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Info"/> level and
        /// appends the specified <see cref="Exception"/>
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="exception">Exception to add to the message</param>
        /// <param name="format">Format string as in
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        [StringFormatMethod("format")]
        public static bool TryInfoFormat(this ILog log, Exception exception, string format, params object[] args)
        {
            return log.TryLogFormat(LogLevel.Info, exception, format, args);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Info"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="message">Message</param>
        /// <param name="meta">Optional semantic meta data</param>
        [Obsolete("Will be dropped in the next release")]
        public static void Info(this ILog log, object message, params XElement[] meta)
        {
            log.Log(LogLevel.Info, message, meta);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Info"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="format">Format string as in 
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        [StringFormatMethod("format")]
        [Obsolete("Will be dropped in the next release")]
        public static void InfoFormat(this ILog log, string format, params object[] args)
        {
            log.Log(LogLevel.Info, string.Format(Culture, format, args));
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Info"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="format">Format string as in 
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        /// <param name="meta">Optional semantic meta data</param>
        [StringFormatMethod("format")]
        [Obsolete("Will be dropped in the next release")]
        public static void InfoFormat(this ILog log, XElement[] meta, string format, params object[] args)
        {
            log.Log(LogLevel.Info, string.Format(Culture, format, args), meta);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Info"/> level and
        /// appends the specified <see cref="Exception"/>
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="ex">Exception to add to the message</param>
        /// <param name="message">Message</param>
        /// <param name="meta">Optional semantic meta data</param>
        [Obsolete("Will be dropped in the next release")]
        public static void Info(this ILog log, Exception ex, object message, params XElement[] meta)
        {
            log.Log(LogLevel.Info, ex, message, meta);
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
        [StringFormatMethod("format")]
        [Obsolete("Will be dropped in the next release")]
        public static void InfoFormat(this ILog log, Exception ex, string format, params object[] args)
        {
            log.Log(LogLevel.Info, ex, string.Format(Culture, format, args));
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
        [StringFormatMethod("format")]
        [Obsolete("Will be dropped in the next release")]
        public static void InfoFormat(this ILog log, Exception ex, XElement[] meta, string format, params object[] args)
        {
            log.Log(LogLevel.Info, ex, string.Format(Culture, format, args), meta);
        }

        /// <summary>
        /// Build a log entry incrementally with a builder
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        public static LogEntryBuilder Warn(this ILog log)
        {
            return new LogEntryBuilder(log, LogLevel.Warn);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Warn"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="message">Message</param>
        /// <param name="exception">Exception to add to the message</param>
        /// <param name="meta">Optional semantic meta data</param>
        public static bool TryWarn(this ILog log, string message, Exception exception = null, XElement meta = null)
        {
            return log.TryLog(LogLevel.Warn, message, exception, meta);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Warn"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="format">Format string as in
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        [StringFormatMethod("format")]
        public static bool TryWarnFormat(this ILog log, string format, params object[] args)
        {
            return log.TryLogFormat(LogLevel.Warn, format, args);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Warn"/> level and
        /// appends the specified <see cref="Exception"/>
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="exception">Exception to add to the message</param>
        /// <param name="format">Format string as in
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        [StringFormatMethod("format")]
        public static bool TryWarnFormat(this ILog log, Exception exception, string format, params object[] args)
        {
            return log.TryLogFormat(LogLevel.Warn, exception, format, args);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Warn"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="message">Message</param>
        /// <param name="meta">Optional semantic meta data</param>
        [Obsolete("Will be dropped in the next release")]
        public static void Warn(this ILog log, object message, params XElement[] meta)
        {
            log.Log(LogLevel.Warn, message, meta);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Warn"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="format">Format string as in 
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        [StringFormatMethod("format")]
        [Obsolete("Will be dropped in the next release")]
        public static void WarnFormat(this ILog log, string format, params object[] args)
        {
            log.Log(LogLevel.Warn, string.Format(Culture, format, args));
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Warn"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="format">Format string as in 
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        /// <param name="meta">Optional semantic meta data</param>
        [StringFormatMethod("format")]
        [Obsolete("Will be dropped in the next release")]
        public static void WarnFormat(this ILog log, XElement[] meta, string format, params object[] args)
        {
            log.Log(LogLevel.Warn, string.Format(Culture, format, args), meta);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Warn"/> level and
        /// appends the specified <see cref="Exception"/>
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="ex">Exception to add to the message</param>
        /// <param name="message">Message</param>
        /// <param name="meta">Optional semantic meta data</param>
        [Obsolete("Will be dropped in the next release")]
        public static void Warn(this ILog log, Exception ex, object message, params XElement[] meta)
        {
            log.Log(LogLevel.Warn, ex, message, meta);
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
        [StringFormatMethod("format")]
        [Obsolete("Will be dropped in the next release")]
        public static void WarnFormat(this ILog log, Exception ex, string format, params object[] args)
        {
            log.Log(LogLevel.Warn, ex, string.Format(Culture, format, args));
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
        [StringFormatMethod("format")]
        [Obsolete("Will be dropped in the next release")]
        public static void WarnFormat(this ILog log, Exception ex, XElement[] meta, string format, params object[] args)
        {
            log.Log(LogLevel.Warn, ex, string.Format(Culture, format, args), meta);
        }


        /// <summary>
        /// Build a log entry incrementally with a builder
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        public static LogEntryBuilder Error(this ILog log)
        {
            return new LogEntryBuilder(log, LogLevel.Error);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Error"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="message">Message</param>
        /// <param name="exception">Exception to add to the message</param>
        /// <param name="meta">Optional semantic meta data</param>
        public static bool TryError(this ILog log, string message, Exception exception = null, XElement meta = null)
        {
            return log.TryLog(LogLevel.Error, message, exception, meta);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Error"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="format">Format string as in
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        [StringFormatMethod("format")]
        public static bool TryErrorFormat(this ILog log, string format, params object[] args)
        {
            return log.TryLogFormat(LogLevel.Error, format, args);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Error"/> level and
        /// appends the specified <see cref="Exception"/>
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="exception">Exception to add to the message</param>
        /// <param name="format">Format string as in
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        [StringFormatMethod("format")]
        public static bool TryErrorFormat(this ILog log, Exception exception, string format, params object[] args)
        {
            return log.TryLogFormat(LogLevel.Error, exception, format, args);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Error"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="message">Message</param>
        /// <param name="meta">Optional semantic meta data</param>
        [Obsolete("Will be dropped in the next release")]
        public static void Error(this ILog log, object message, params XElement[] meta)
        {
            log.Log(LogLevel.Error, message, meta);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Error"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="format">Format string as in 
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        [StringFormatMethod("format")]
        [Obsolete("Will be dropped in the next release")]
        public static void ErrorFormat(this ILog log, string format, params object[] args)
        {
            log.Log(LogLevel.Error, string.Format(Culture, format, args));
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Error"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="format">Format string as in 
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        /// <param name="meta">Optional semantic meta data</param>
        [StringFormatMethod("format")]
        [Obsolete("Will be dropped in the next release")]
        public static void ErrorFormat(this ILog log, XElement[] meta, string format, params object[] args)
        {
            log.Log(LogLevel.Error, string.Format(Culture, format, args), meta);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Error"/> level and
        /// appends the specified <see cref="Exception"/>
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="ex">Exception to add to the message</param>
        /// <param name="message">Message</param>
        /// <param name="meta">Optional semantic meta data</param>
        [Obsolete("Will be dropped in the next release")]
        public static void Error(this ILog log, Exception ex, object message, params XElement[] meta)
        {
            log.Log(LogLevel.Error, ex, message, meta);
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
        [StringFormatMethod("format")]
        [Obsolete("Will be dropped in the next release")]
        public static void ErrorFormat(this ILog log, Exception ex, string format, params object[] args)
        {
            log.Log(LogLevel.Error, ex, string.Format(Culture, format, args));
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
        [StringFormatMethod("format")]
        [Obsolete("Will be dropped in the next release")]
        public static void ErrorFormat(this ILog log, Exception ex, XElement[] meta, string format, params object[] args)
        {
            log.Log(LogLevel.Error, ex, string.Format(Culture, format, args), meta);
        }

        /// <summary>
        /// Build a log entry incrementally with a builder
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        public static LogEntryBuilder Fatal(this ILog log)
        {
            return new LogEntryBuilder(log, LogLevel.Fatal);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Fatal"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="message">Message</param>
        /// <param name="exception">Exception to add to the message</param>
        /// <param name="meta">Optional semantic meta data</param>
        public static bool TryFatal(this ILog log, string message, Exception exception = null, XElement meta = null)
        {
            return log.TryLog(LogLevel.Fatal, message, exception, meta);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Fatal"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="format">Format string as in
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        [StringFormatMethod("format")]
        public static bool TryFatalFormat(this ILog log, string format, params object[] args)
        {
            return log.TryLogFormat(LogLevel.Fatal, format, args);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Fatal"/> level and
        /// appends the specified <see cref="Exception"/>
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="exception">Exception to add to the message</param>
        /// <param name="format">Format string as in
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        [StringFormatMethod("format")]
        public static bool TryFatalFormat(this ILog log, Exception exception, string format, params object[] args)
        {
            return log.TryLogFormat(LogLevel.Fatal, exception, format, args);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Fatal"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="message">Message</param>
        /// <param name="meta">Optional semantic meta data</param>
        [Obsolete("Will be dropped in the next release")]
        public static void Fatal(this ILog log, object message, params XElement[] meta)
        {
            log.Log(LogLevel.Fatal, message, meta);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Fatal"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="format">Format string as in 
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        [StringFormatMethod("format")]
        [Obsolete("Will be dropped in the next release")]
        public static void FatalFormat(this ILog log, string format, params object[] args)
        {
            log.Log(LogLevel.Fatal, string.Format(Culture, format, args));
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Fatal"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="format">Format string as in 
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        /// <param name="meta">Optional semantic meta data</param>
        [StringFormatMethod("format")]
        [Obsolete("Will be dropped in the next release")]
        public static void FatalFormat(this ILog log, XElement[] meta, string format, params object[] args)
        {
            log.Log(LogLevel.Fatal, string.Format(Culture, format, args), meta);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Fatal"/> level and
        /// appends the specified <see cref="Exception"/>
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="ex">Exception to add to the message</param>
        /// <param name="message">Message</param>
        /// <param name="meta">Optional semantic meta data</param>
        [Obsolete("Will be dropped in the next release")]
        public static void Fatal(this ILog log, Exception ex, object message, params XElement[] meta)
        {
            log.Log(LogLevel.Fatal, ex, message, meta);
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
        [StringFormatMethod("format")]
        [Obsolete("Will be dropped in the next release")]
        public static void FatalFormat(this ILog log, Exception ex, string format, params object[] args)
        {
            log.Log(LogLevel.Fatal, ex, string.Format(Culture, format, args));
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
        [StringFormatMethod("format")]
        [Obsolete("Will be dropped in the next release")]
        public static void FatalFormat(this ILog log, Exception ex, XElement[] meta, string format, params object[] args)
        {
            log.Log(LogLevel.Fatal, ex, string.Format(Culture, format, args), meta);
        }
    }
}
