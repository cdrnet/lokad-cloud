using System;
using System.Xml.Linq;

namespace Lokad.Cloud.Diagnostics
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

        internal static string ValueOrDefault(this XElement element)
        {
            return element == null ? null : element.Value;
        }
    }
}
