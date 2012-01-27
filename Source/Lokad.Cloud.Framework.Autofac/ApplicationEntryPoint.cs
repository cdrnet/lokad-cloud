#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Autofac;
using Autofac.Configuration;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.EntryPoint;
using Lokad.Cloud.ServiceFabric;
using Microsoft.WindowsAzure;

namespace Lokad.Cloud.Framework.Autofac
{
    /// <summary>
    /// Custom EntryPoint for service creation and extension using Autofac IoC
    /// </summary>
    public class ApplicationEntryPoint : IApplicationEntryPoint
    {
        private IApplicationEnvironment Environment { get; set; }
        private XElement Settings { get; set; }

        public void Run(XElement settings, IDeploymentReader deploymentReader, IApplicationEnvironment environment, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            Environment = environment;
            Settings = settings;

            var builder = new ContainerBuilder();
            builder.RegisterModule<AzureModule>();
            builder.RegisterInstance(CloudStorageAccount.Parse(Settings.Element("DataConnectionString").Value));
            builder.RegisterInstance(Environment);
            builder.RegisterType<CloudServiceRunner>();
            InjectRegistration(builder);

            // Load Application IoC Configuration and apply it to the builder (allows users to override and extend)
            var autofacXml = Settings.Element("RawConfig");
            var autofacConfig = autofacXml != null && !string.IsNullOrEmpty(autofacXml.Value)
                ? Convert.FromBase64String(autofacXml.Value)
                : null;
            if (autofacConfig != null && autofacConfig.Length > 0)
            {
                // HACK: need to copy settings locally first
                // HACK: hard-code string for local storage name
                const string fileName = "lokad.cloud.clientapp.config";
                const string resourceName = "LokadCloudStorage";

                var pathToFile = Path.Combine(Environment.GetLocalResourcePath(resourceName), fileName);
                File.WriteAllBytes(pathToFile, autofacConfig);
                builder.RegisterModule(new ConfigurationSettingsReader("autofac", pathToFile));
            }

            // Look for all cloud services currently loaded in the AppDomain
            var serviceTypes = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetExportedTypes()).SelectMany(x => x)
                .Where(t => t.IsSubclassOf(typeof(CloudService)) && !t.IsAbstract && !t.IsGenericType)
                .ToList();

            // Register the cloud services in the IoC Builder so we can support dependencies
            foreach (var type in serviceTypes)
            {
                builder.RegisterType(type)
                    .OnActivating(e =>
                    {
                        e.Context.InjectUnsetProperties(e.Instance);

                        var initializable = e.Instance as IInitializable;
                        if (initializable != null)
                        {
                            initializable.Initialize();
                        }
                    })
                    .InstancePerDependency()
                    .ExternallyOwned();

                // ExternallyOwned: to prevent the container from disposing the
                // cloud services - we manage their lifetime on our own using
                // e.g. RuntimeFinalizer
            }

            using (var applicationContainer = builder.Build())
            {
                // Instanciate and return all the cloud services
                var services = serviceTypes.Select(type => (CloudService)applicationContainer.Resolve(type)).ToList();

                var runner = applicationContainer.Resolve<CloudServiceRunner>();

                // Instanciate and return all the cloud services
                runner.Run(environment, services, cancellationToken);
            }
        }

        public void OnSettingsChanged(XElement settings)
        {
        }

        /// <summary>
        /// Override this method to register additional types or modules,
        /// to be injected later into your services.
        /// </summary>
        protected virtual void InjectRegistration(ContainerBuilder builder)
        {
        }
    }
}