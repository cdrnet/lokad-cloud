#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Collections.Generic;
using System.Linq;
using Lokad.Cloud.Services.Framework;

namespace Lokad.Cloud.Services.Runtime.Runner
{
    internal class CommonServiceRunner
    {
        private readonly List<ICloudService> _services;

        public CommonServiceRunner(IEnumerable<ICloudService> services)
        {
            _services = services.ToList();
        }

        public void Initialize()
        {
            foreach (var service in _services)
            {
                service.Initialize();
            }
        }

        public void Start()
        {
            foreach (var service in _services)
            {
                service.OnStart();
            }
        }

        public void Stop()
        {
            foreach (var service in _services)
            {
                service.OnStop();
            }
        }
    }
}
