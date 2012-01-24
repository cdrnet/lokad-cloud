#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Xml.Linq;

namespace Lokad.Cloud.Diagnostics
{
    /// <summary>
    /// Basic log writer abstraction.
    /// </summary>
    public interface ILog
    {
        /// <summary>Writes the message to the logger</summary>
        /// <param name="level">The importance level</param>
        /// <param name="message">The actual message</param>
        /// <param name="exception">The actual exception</param>
        /// <param name="meta">Optional semantic meta data</param>
        void Log(LogLevel level, string message, Exception exception = null, XElement meta = null);
    }
}
