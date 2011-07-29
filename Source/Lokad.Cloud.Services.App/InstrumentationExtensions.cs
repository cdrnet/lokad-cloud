using System;
using Lokad.Cloud.Services.Framework.Instrumentation;
using Lokad.Cloud.Services.Framework.Instrumentation.Events;

namespace Lokad.Cloud.Services.Runtime
{
    internal static class InstrumentationExtensions
    {
        public static void TryNotify(this ICloudRuntimeObserver observer, Func<ICloudRuntimeEvent> @event)
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
