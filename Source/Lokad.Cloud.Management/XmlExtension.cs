#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Xml.Linq;

namespace Lokad.Cloud.Management
{
    internal static class XmlExtension
    {
        internal static T ProjectOrDefault<T>(this XElement element, Func<XElement, T> projection, Func<T> defaultResult)
        {
            return element == null ? defaultResult() : projection(element);
        }

        internal static T ProjectOrDefault<T>(this XElement element, Func<XElement, T> projection)
        {
            return element == null ? default(T) : projection(element);
        }

        internal static string ValueOrEmpty(this XElement element)
        {
            return element == null ? string.Empty : element.Value;
        }

        internal static string ValueOrDefault(this XElement element)
        {
            return element == null ? null : element.Value;
        }
    }
}
