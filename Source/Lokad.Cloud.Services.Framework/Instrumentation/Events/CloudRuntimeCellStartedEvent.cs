﻿#region Copyright (c) Lokad 2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

namespace Lokad.Cloud.Services.Framework.Instrumentation.Events
{
    /// <summary>
    /// Raised whenever the runtime is started
    /// </summary>
    public class CloudRuntimeCellStartedEvent : ICloudRuntimeEvent
    {
        public string CellName { get; set; }

        public CloudRuntimeCellStartedEvent(string cellName)
        {
            CellName = cellName;
        }
    }
}
