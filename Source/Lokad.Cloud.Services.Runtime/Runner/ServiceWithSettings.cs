#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Xml.Linq;
using Lokad.Cloud.Services.Framework;

namespace Lokad.Cloud.Services.Runtime.Runner
{
    public class ServiceWithSettings<TService>
        where TService : ICloudService
    {
        public TService Service { get; private set; }
        public XElement Settings { get; private set; }

        public ServiceWithSettings(TService service, XElement settings)
        {
            Service = service;
            Settings = settings;
        }
    }
}
