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
    public class AutofacApplicationEntryPoint : IApplicationEntryPoint
    {
        private CancellationTokenSource _cancelledOrSettingsChangedCts;

        protected IApplicationEnvironment Environment { get; private set; }
        protected IEnvironment CloudEnvironment { get; private set; }
        protected XElement Settings { get; private set; }
        protected string DataConnectionString { get; set; }

        public void Run(XElement settings, IDeploymentReader deploymentReader, IApplicationEnvironment environment, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            
            Environment = environment;
            CloudEnvironment = new EnvironmentAdapter(environment);
            Settings = settings;
            DataConnectionString = Settings.Element("DataConnectionString").Value;

            // We want to recycle if the settings change, but not actually unload the AppDomain.
            // That's why we loop here instead of just return back to the caller.
            // This is purely custom behavior, change in your EntryPoint as you see fit.
            while (!cancellationToken.IsCancellationRequested)
            {
                _cancelledOrSettingsChangedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                var builder = new ContainerBuilder();
                builder.RegisterModule<AzureModule>();
                builder.RegisterInstance(CloudStorageAccount.Parse(DataConnectionString));
                builder.RegisterInstance(CloudEnvironment);
                builder.RegisterType<CloudServiceRunner>();
                InjectRegistration(builder);

                // Load Application IoC Configuration and apply it to the builder (allows users to override and extend)
                var autofacXml = settings.Element("RawConfig");
                var autofacConfig = autofacXml != null && !string.IsNullOrEmpty(autofacXml.Value)
                    ? Convert.FromBase64String(autofacXml.Value)
                    : null;
                if (autofacConfig != null && autofacConfig.Length > 0)
                {
                    // HACK: need to copy settings locally first
                    // HACK: hard-code string for local storage name
                    const string fileName = "lokad.cloud.clientapp.config";
                    const string resourceName = "LokadCloudStorage";

                    var pathToFile = Path.Combine(environment.GetLocalResourcePath(resourceName), fileName);
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
                    var services = serviceTypes.Select(type => (CloudService)applicationContainer.Resolve(type)).ToList();
                    var runner = applicationContainer.Resolve<CloudServiceRunner>();

                    runner.Run(CloudEnvironment, services, _cancelledOrSettingsChangedCts.Token);
                }
            }
        }

        public void OnSettingsChanged(XElement settings)
        {
            var cts = _cancelledOrSettingsChangedCts;
            if (cts != null)
            {
                cts.Cancel();
            }
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