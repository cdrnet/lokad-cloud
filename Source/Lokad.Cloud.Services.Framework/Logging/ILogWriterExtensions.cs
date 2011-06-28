#region (c)2009-2011 Lokad - New BSD license
// Company: http://www.lokad.com
// This code is released under the terms of the new BSD licence
#endregion

using System;
using System.Globalization;

namespace Lokad.Cloud.Services.Framework.Logging
{
    /// <summary>
    /// Helper extensions for any class that implements <see cref="ILogWriter"/>
    /// </summary>
    public static class ILogWriterExtensions
    {
        static readonly CultureInfo _culture = CultureInfo.InvariantCulture;

        /// <summary>
        /// Writes message with <see cref="LogLevel.Debug"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="message">Message</param>
        public static void Debug(this ILogWriter log, object message)
        {
            log.Log(LogLevel.Debug, message);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Debug"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="format">Format string as in 
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        public static void DebugFormat(this ILogWriter log, string format, params object[] args)
        {
            log.Log(LogLevel.Debug, string.Format(_culture, format, args));
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Debug"/> level and
        /// appends the specified <see cref="Exception"/>
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="ex">Exception to add to the message</param>
        /// <param name="message">Message</param>
        public static void Debug(this ILogWriter log, Exception ex, object message)
        {
            log.Log(LogLevel.Debug, ex, message);
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
        public static void DebugFormat(this ILogWriter log, Exception ex, string format, params object[] args)
        {
            log.Log(LogLevel.Debug, ex, string.Format(_culture, format, args));
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Info"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="message">Message</param>
        public static void Info(this ILogWriter log, object message)
        {
            log.Log(LogLevel.Info, message);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Info"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="format">Format string as in 
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        public static void InfoFormat(this ILogWriter log, string format, params object[] args)
        {
            log.Log(LogLevel.Info, string.Format(_culture, format, args));
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Info"/> level and
        /// appends the specified <see cref="Exception"/>
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="ex">Exception to add to the message</param>
        /// <param name="message">Message</param>
        public static void Info(this ILogWriter log, Exception ex, object message)
        {
            log.Log(LogLevel.Info, ex, message);
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
        public static void InfoFormat(this ILogWriter log, Exception ex, string format, params object[] args)
        {
            log.Log(LogLevel.Info, ex, string.Format(_culture, format, args));
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Warn"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="message">Message</param>
        public static void Warn(this ILogWriter log, object message)
        {
            log.Log(LogLevel.Warn, message);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Warn"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="format">Format string as in 
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        public static void WarnFormat(this ILogWriter log, string format, params object[] args)
        {
            log.Log(LogLevel.Warn, string.Format(_culture, format, args));
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Warn"/> level and
        /// appends the specified <see cref="Exception"/>
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="ex">Exception to add to the message</param>
        /// <param name="message">Message</param>
        public static void Warn(this ILogWriter log, Exception ex, object message)
        {
            log.Log(LogLevel.Warn, ex, message);
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
        public static void WarnFormat(this ILogWriter log, Exception ex, string format, params object[] args)
        {
            log.Log(LogLevel.Warn, ex, string.Format(_culture, format, args));
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Error"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="message">Message</param>
        public static void Error(this ILogWriter log, object message)
        {
            log.Log(LogLevel.Error, message);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Error"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="format">Format string as in 
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        public static void ErrorFormat(this ILogWriter log, string format, params object[] args)
        {
            log.Log(LogLevel.Error, string.Format(_culture, format, args));
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Error"/> level and
        /// appends the specified <see cref="Exception"/>
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="ex">Exception to add to the message</param>
        /// <param name="message">Message</param>
        public static void Error(this ILogWriter log, Exception ex, object message)
        {
            log.Log(LogLevel.Error, ex, message);
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
        public static void ErrorFormat(this ILogWriter log, Exception ex, string format, params object[] args)
        {
            log.Log(LogLevel.Error, ex, string.Format(_culture, format, args));
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Fatal"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="message">Message</param>
        public static void Fatal(this ILogWriter log, object message)
        {
            log.Log(LogLevel.Fatal, message);
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Fatal"/> level
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="format">Format string as in 
        /// <see cref="string.Format(string,object[])"/></param>
        /// <param name="args">Arguments</param>
        public static void FatalFormat(this ILogWriter log, string format, params object[] args)
        {
            log.Log(LogLevel.Fatal, string.Format(_culture, format, args));
        }

        /// <summary>
        /// Writes message with <see cref="LogLevel.Fatal"/> level and
        /// appends the specified <see cref="Exception"/>
        /// </summary>
        /// <param name="log">Log instance being extended</param>
        /// <param name="ex">Exception to add to the message</param>
        /// <param name="message">Message</param>
        public static void Fatal(this ILogWriter log, Exception ex, object message)
        {
            log.Log(LogLevel.Fatal, ex, message);
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
        public static void FatalFormat(this ILogWriter log, Exception ex, string format, params object[] args)
        {
            log.Log(LogLevel.Fatal, ex, string.Format(_culture, format, args));
        }
    }
}
