#region (c)2009-2011 Lokad - New BSD license
// Company: http://www.lokad.com
// This code is released under the terms of the new BSD licence
#endregion

using System;

namespace Lokad.Cloud.Services.Framework.Logging
{
    /// <summary>
    /// Basic logging abstraction.
    /// </summary>
    public interface ILogWriter
    {
        /// <summary> Writes the message to the logger </summary>
        /// <param name="level">The importance level</param>
        /// <param name="message">The actual message</param>
        void Log(LogLevel level, object message);

        /// <summary>
        /// Writes the exception and associated information 
        /// to the logger
        /// </summary>
        /// <param name="level">The importance level</param>
        /// <param name="ex">The actual exception</param>
        /// <param name="message">Information related to the exception</param>
        void Log(LogLevel level, Exception ex, object message);
    }
}
