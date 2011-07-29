using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Lokad.Cloud.Services.Runtime.WorkingSet;

namespace Lokad.Cloud.Services.Runtime.Internal
{
    internal static class SettingsTools
    {
        internal static IEnumerable<CellArrangement> DenormalizeSettingsByCell(XElement serviceSettings)
        {
            return serviceSettings.Elements("Service")
                .Where(service => service.AttributeValue("disabled") != "true")
                .SelectMany(service => service.SettingsElements("CellAffinity", "Cell").Select(cell => Tuple.Create(cell.AttributeValue("name"), service)))
                .GroupBy(tuple => tuple.Item1, tuple => tuple.Item2)
                .Select(group => new CellArrangement(group.Key, new XElement("Services", group)));
        }

        //internal static Delta<string> 
    }

    internal class Delta<TKey>
    {
    }
}
