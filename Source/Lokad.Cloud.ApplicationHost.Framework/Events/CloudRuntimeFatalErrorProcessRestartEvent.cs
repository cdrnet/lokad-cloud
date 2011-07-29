#region Copyright (c) Lokad 2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;

namespace Lokad.Cloud.AppHost.Framework.Events
{
    /// <summary>
    /// Raised whenever the a runtime process restarts because of an fatal error.
    /// </summary>
    public class CloudRuntimeFatalErrorProcessRestartEvent : IHostEvent
    {
        public Exception Exception { get; private set; }
        public string CellName { get; private set; }

        public CloudRuntimeFatalErrorProcessRestartEvent(Exception exception, string cellName)
        {
            Exception = exception;
            CellName = cellName;
        }
    }
}
