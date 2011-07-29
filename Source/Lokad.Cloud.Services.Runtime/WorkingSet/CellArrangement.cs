#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Xml.Linq;

namespace Lokad.Cloud.Services.Runtime.WorkingSet
{
    internal sealed class CellArrangement
    {
        internal string CellName { get; private set; }
        internal XElement ServicesSettings { get; private set; }

        internal CellArrangement(string cellName, XElement serviceSettings)
        {
            CellName = cellName;
            ServicesSettings = serviceSettings;
        }
    }
}
