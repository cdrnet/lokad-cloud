#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Autofac;
using Autofac.Configuration;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.Services.App.Util;
using Lokad.Cloud.Services.Framework;
using Lokad.Cloud.Services.Management;

namespace Lokad.Cloud.Services.App
{
    internal class ServiceContainer : IDisposable
    {
        private readonly IContainer _container;
        private readonly List<Type> _types;

        public ServiceContainer(byte[] config, IApplicationEnvironment environment)
        {
            var applicationBuilder = new ContainerBuilder();
            applicationBuilder.RegisterModule(new CloudModule());
            applicationBuilder.RegisterModule(new ManagementModule());
            applicationBuilder.RegisterInstance(new EnvironmentAdapter(environment)).As<ICloudEnvironment>();
            // TODO: applicationBuilder.RegisterInstance(_settings);

            // Load Application IoC Configuration and apply it to the builder
            if (config != null && config.Length > 0)
            {
                // the configuration manager expects a file in the file system,
                // so we need to copy it to a local file first
                const string fileName = "lokad.cloud.clientapp.config";
                const string resourceName = "LokadCloudStorage";

                var pathToFile = Path.Combine(CloudEnvironment.GetLocalStoragePath(resourceName), fileName);
                File.WriteAllBytes(pathToFile, config);
                applicationBuilder.RegisterModule(new ConfigurationSettingsReader("autofac", pathToFile));
            }

            // Look for all cloud services currently loaded in the AppDomain
            _types = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetExportedTypes()).SelectMany(x => x)
                .Where(t => typeof(ICloudService).IsAssignableFrom(t) && !t.IsAbstract && !t.IsGenericType)
                .ToList();

            // Register the cloud services in the IoC Builder so we can support dependencies
            foreach (var type in _types)
            {
                applicationBuilder.RegisterType(type)
                    .PropertiesAutowired(PropertyWiringFlags.PreserveSetValues)
                    .InstancePerDependency()
                    .ExternallyOwned();

                // ExternallyOwned: to prevent the container from disposing the
                // cloud services - we manage their lifetime on our own using
                // e.g. RuntimeFinalizer
            }

            _container = applicationBuilder.Build();
        }

        /// <summary>
        /// Matches and resolves services with their settings but skip disabled services and services without settings.
        /// </summary>
        public IEnumerable<Framework.Runner.ServiceWithSettings<TService>> ResolveServices<TService>(IEnumerable<XElement> serviceXmls)
            where TService : ICloudService
        {
            var serviceXmlByImpTypeName = serviceXmls.ToDictionary(s => s.SettingsElement("ImplementationType").AttributeValue("name"));
            foreach (var type in _types)
            {
                XElement serviceXml;
                if (serviceXmlByImpTypeName.TryGetValue(type.FullName, out serviceXml) && serviceXml.AttributeValue("disabled") != "true")
                {
                    yield return new Framework.Runner.ServiceWithSettings<TService>((TService)_container.Resolve(type), serviceXml);
                }
            }
        }

        public void Dispose()
        {
            _container.Dispose();
        }
    }
}
