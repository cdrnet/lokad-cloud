#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Xml.Linq;
using Lokad.Cloud.Diagnostics;

namespace Lokad.Cloud.Stubs
{
    /// <summary> <see cref="ILog"/> that does not do anything.</summary>
    [Serializable]
    public sealed class StubLog : ILog
    {
        /// <summary>
        /// Singleton instance of the <see cref="StubLog"/>.
        /// </summary>
        public static readonly ILog Instance = new StubLog();

        StubLog()
        {
        }

        void ILog.Log(LogLevel level, string message, Exception exception, XElement meta)
        {
        }
    }
}
