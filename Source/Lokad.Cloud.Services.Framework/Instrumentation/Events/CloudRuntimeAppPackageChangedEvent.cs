﻿#region Copyright (c) Lokad 2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;

namespace Lokad.Cloud.Services.Framework.Instrumentation.Events
{
    /// <summary>
    /// Raised whenever the runtime detects any application package assemblies change
    /// </summary>
    public class CloudRuntimeAppPackageChangedEvent : ICloudRuntimeEvent
    {
        public CloudRuntimeAppPackageChangedEvent()
        {
        }
    }
}
