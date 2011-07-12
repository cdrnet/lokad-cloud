#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.Services.Framework;
using Lokad.Cloud.Services.Management.Settings;

namespace Lokad.Cloud.Services.Runtime.Runner
{
    public class ServiceWithSettings<TService, TSettings>
        where TService : ICloudService
        where TSettings : CommonServiceSettings
    {
        public TService Service { get; private set; }
        public TSettings Settings { get; private set; }

        public ServiceWithSettings(TService service, TSettings settings)
        {
            Service = service;
            Settings = settings;
        }
    }
}
