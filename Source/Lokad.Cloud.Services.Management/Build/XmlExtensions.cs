#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Xml.Linq;

namespace Lokad.Cloud.Services.Management.Build
{
    internal static class XmlExtensions
    {
        public static string AttributeValue(this XElement container, string attributeName)
        {
            var attribute = container.Attribute(attributeName);
            if (attribute == null)
            {
                return null;
            }

            return attribute.Value;
        }
    }
}
