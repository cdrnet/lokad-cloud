#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

namespace Lokad.Cloud.Services.Runtime.Internal
{ 
    /// <summary>
    /// Cell-specific counterpart of the RuntimeHandle.
    /// Handle to communicate with the runtime from the outside:
    /// - Send commands to the runtime
    /// - Report (stale) info about the runtime to the outside (like current deployment)
    /// </summary>
    internal class CellHandle
    {
        internal readonly string CellName;

        internal CellHandle(string cellName)
        {
            CellName = cellName;
        }
    }
}
