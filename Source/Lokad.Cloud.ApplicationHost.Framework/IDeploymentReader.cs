#region Copyright (c) Lokad 2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Xml.Linq;

namespace Lokad.Cloud.AppHost.Framework
{
    /// <remarks>Implementation needs to be serializable to be able to cross AppDomains.</remarks>
    public interface IDeploymentReader
    {
        XElement GetHeadIfModified(string knownETag, out string newETag);

        /// <summary>
        /// Expected to support at least byte[] and XElement for T.
        /// </summary>
        T GetItem<T>(string itemName) where T : class;
    }
}
