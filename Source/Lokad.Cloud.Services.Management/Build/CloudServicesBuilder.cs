#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Xml.Linq;

namespace Lokad.Cloud.Services.Management.Build
{
    public class CloudServicesBuilder
    {
        private readonly CellBuilder _cellBuilder;
        private readonly DeploymentWriter _writer;
        private readonly XElement _xml;

        internal CloudServicesBuilder(CellBuilder cellBuilder, DeploymentWriter writer, XElement servicesXml)
        {
            _cellBuilder = cellBuilder;
            _xml = servicesXml;
            _writer = writer;
        }

        public void ExistingServices(string servicesName)
        {
            _xml.RemoveAll();
            _xml.SetAttributeValue("name", servicesName);
        }
    }
}
