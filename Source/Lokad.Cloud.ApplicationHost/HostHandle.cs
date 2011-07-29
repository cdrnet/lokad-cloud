#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Net;
using Lokad.Cloud.AppHost.Framework;

namespace Lokad.Cloud.AppHost
{
    /// <summary>
    /// Handle to communicate with the application host from the outside and the inside:
    /// - Send commands to the host
    /// - Query (stale) info about the host (like current deployment)
    /// </summary>
    internal class HostHandle
    {
        private readonly IHostObserver _observer;

        internal readonly Action<IHostCommand> SendCommand;
        internal readonly Lazy<string> MachineName = new Lazy<string>(Dns.GetHostName);

        internal HostHandle(Action<IHostCommand> sendCommand, IHostObserver observer)
        {
            _observer = observer;
            SendCommand = sendCommand;
        }

        public void TryNotify(Func<IHostEvent> @event)
        {
            // TODO: Consider to drop from handle again, is not really a runtime concern

            if (_observer != null)
            {
                try
                {
                    _observer.Notify(@event());
                }
                catch
                {
                    // Suppression is intended: we can't log but also don't want to tear down just because of a failed notification
                }
            }
        }
    }
}
