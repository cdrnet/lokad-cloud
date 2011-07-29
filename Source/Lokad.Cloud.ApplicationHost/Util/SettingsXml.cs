#region Copyright (c) Lokad 2010-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Lokad.Cloud.AppHost.Util
{
    internal static class SettingsXml
    {
        public static XElement SettingsElement(this XContainer container, string elementName)
        {
            var element = container.Element(elementName);
            if (element == null)
            {
                throw new ArgumentException(string.Format("Settings XML: element '{0}' has no child element '{1}'", container, elementName));
            }

            return element;
        }

        public static IEnumerable<XElement> SettingsElements(this XContainer container, string parentElementName, string itemElementName)
        {
            var parentElement = container.Element(parentElementName);
            if (parentElement == null)
            {
                return new XElement[0];
            }

            return parentElement.Elements(itemElementName);
        }

        public static string SettingsElementAttributeValue(this XContainer container, string elementName, string attributeName)
        {
            var element = container.Element(elementName);
            if (element == null)
            {
                return null;
            }

            return element.AttributeValue(attributeName);
        }

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
