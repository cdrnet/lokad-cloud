#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Lokad.Cloud.Services.Framework.Runner
{
    internal class CommonServiceRunner<TService>
        where TService : ICloudService
    {
        private readonly List<ServiceWithSettings<TService>> _services;

        public CommonServiceRunner(IEnumerable<ServiceWithSettings<TService>> services)
        {
            _services = services.ToList();
        }

        public void Initialize()
        {
            foreach (var service in _services)
            {
                service.Service.Initialize(service.ServiceXml.Element("UserSettings") ?? new XElement("UserSettings"));
            }
        }

        public void Start()
        {
            foreach (var service in _services)
            {
                service.Service.OnStart();
            }
        }

        public void Stop()
        {
            foreach (var service in _services)
            {
                service.Service.OnStop();
            }
        }
    }
}
