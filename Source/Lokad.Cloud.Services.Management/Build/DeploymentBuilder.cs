#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using System.Xml.Linq;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.Services.Management.Build
{
    public class DeploymentBuilder
    {
        private readonly DeploymentReader _reader;
        private readonly DeploymentWriter _writer;

        private XElement _root;
        private XElement _cells;

        public DeploymentBuilder(CloudStorageProviders storage)
        {
            _reader = new DeploymentReader(storage);
            _writer = new DeploymentWriter(storage);

            _root = new XElement("Deployment", _cells = new XElement("Cells"));
        }

        public void FromExistingDeployment(string deploymentName)
        {
            _root = _reader.ReadDeploymentXml(deploymentName);
            _cells = _root.Element("Cells");
            if (_cells == null)
            {
                _root.Add(_cells = new XElement("Cells"));
            }
        }

        public void Cell(string cellName, Action<CellBuilder> configure)
        {
            var cell = GetCellXmlIfExists(cellName);
            if (cell == null)
            {
                _cells.Add(cell = new XElement("Cell", new XAttribute("name", cellName)));
            }

            configure(new CellBuilder(this, _writer, _reader, cell));
        }

        public void RemoveCellIfExists(string cellName)
        {
            var cellToRemove = GetCellXmlIfExists(cellName);
            if (cellToRemove != null)
            {
                cellToRemove.Remove();
            }
        }

        internal XElement GetCellXmlIfExists(string cellName)
        {
            return _cells.Elements("Cell").FirstOrDefault(x => x.AttributeValue("name") == cellName);
        }

        public string Publish()
        {
            throw new NotImplementedException();
        }

        public string DeployAsHead()
        {
            throw new NotImplementedException();
        }
    }
}
