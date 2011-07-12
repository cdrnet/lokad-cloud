#region Copyright (c) Lokad 2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

namespace Lokad.Cloud.Services.Framework.Instrumentation.Events
{
    /// <summary>
    /// Raised whenever the runtime detects any service settings change
    /// </summary>
    public class CloudRuntimeServiceSettingsChangedEvent : ICloudRuntimeEvent
    {
        public CloudRuntimeServiceSettingsChangedEvent()
        {
        }
    }
}
