#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Xml.Linq;

namespace Lokad.Cloud.Services.Management.Build
{
    public class AutofacConfigBuilder
    {
        private readonly CellBuilder _cellBuilder;
        private readonly DeploymentWriter _writer;
        private readonly XElement _xml;

        internal AutofacConfigBuilder(CellBuilder cellBuilder, DeploymentWriter writer, XElement configXml)
        {
            _cellBuilder = cellBuilder;
            _xml = configXml;
            _writer = writer;
        }

        public void ExistingAutofacConfig(string configName)
        {
            _xml.RemoveAll();
            _xml.SetAttributeValue("name", configName);
        }
    }
}
