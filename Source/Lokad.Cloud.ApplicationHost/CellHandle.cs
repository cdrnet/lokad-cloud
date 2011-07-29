#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

namespace Lokad.Cloud.AppHost
{ 
    /// <summary>
    /// Cell-specific counterpart of the HostHandle.
    /// Handle to communicate with the application host from the outside and the inside:
    /// - Send commands to the host
    /// - Query (stale) info about the host (like current deployment)
    /// </summary>
    internal class CellHandle
    {
        internal readonly string CellName;

        internal string CurrentDeploymentName { get; set; }
        internal string CurretAssembliesName { get; set; }

        internal CellHandle(string cellName)
        {
            CellName = cellName;
        }
    }
}
