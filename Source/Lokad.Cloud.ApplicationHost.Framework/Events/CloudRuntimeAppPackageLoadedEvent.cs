#region Copyright (c) Lokad 2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;

namespace Lokad.Cloud.AppHost.Framework.Events
{
    /// <summary>
    /// Raised whenever the runtime detects any application package assemblies change
    /// </summary>
    public class CloudRuntimeAppPackageLoadedEvent : IHostEvent
    {
        public DateTimeOffset PackageTimestamp { get; private set; }
        public string PackageETag { get; private set; }

        public CloudRuntimeAppPackageLoadedEvent(DateTimeOffset packageTimestamp, string packageETag)
        {
            PackageTimestamp = packageTimestamp;
            PackageETag = packageETag;
        }
    }
}
