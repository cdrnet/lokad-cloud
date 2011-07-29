#region Copyright (c) Lokad 2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using Lokad.Cloud.AppHost.Framework;

namespace Lokad.Cloud.AppHost.Util
{
    internal static class InstrumentationExtensions
    {
        public static void TryNotify(this IHostObserver observer, Func<IHostEvent> @event)
        {
            if (observer != null)
            {
                try
                {
                    observer.Notify(@event());
                }
                catch
                {
                    // Suppression is intended: we can't log but also don't want to tear down just because of a failed notification
                }
            }
        }
    }
}
