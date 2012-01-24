using System;
using System.Globalization;
using System.Xml.Linq;

namespace Lokad.Cloud.Diagnostics
{
    /// <summary>
    /// Helper extensions for any class that implements <see cref="ILog"/>
    /// </summary>
    public static class LogExtensions
    {
        static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

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
        /// <param name="meta">Optional semantic meta data</param>
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
        /// <param name="meta">Optional semantic meta data</param>
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
        /// <param name="meta">Optional semantic meta data</param>
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
        /// <param name="meta">Optional semantic meta data</param>
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
        /// <param name="meta">Optional semantic meta data</param>
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
        public static void FatalFormat(this ILog log, Exception ex, XElement[] meta, string format, params object[] args)
        {
            log.Log(LogLevel.Fatal, ex, string.Format(Culture, format, args), meta);
        }
    }
}