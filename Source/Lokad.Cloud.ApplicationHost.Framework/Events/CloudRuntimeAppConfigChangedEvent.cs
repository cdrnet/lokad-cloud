#region Copyright (c) Lokad 2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

namespace Lokad.Cloud.AppHost.Framework.Events
{
    /// <summary>
    /// Raised whenever the runtime detects any application package configuration change
    /// </summary>
    public class CloudRuntimeAppConfigChangedEvent : IHostEvent
    {
        public CloudRuntimeAppConfigChangedEvent()
        {
        }
    }
}
