#region Copyright (c) Lokad 2009-2012
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
using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.Instrumentation;
using Lokad.Cloud.Provisioning.Instrumentation;
using Lokad.Cloud.Provisioning.Instrumentation.Events;
using Lokad.Cloud.ServiceFabric;
using Lokad.Cloud.Storage;
using Lokad.Cloud.Storage.Azure;
using Microsoft.WindowsAzure;

namespace Lokad.Cloud.EntryPoint
{
    // NOTE: referred to by name in WorkerRole DeploymentReader
    public class AutofacCloudFactory : ICloudFactory, IDisposable
    {
        private IApplicationEnvironment _environment;
        private string _connectionString;
        private byte[] _autofacConfig;

        private IDisposable _disposable;

        public void Initialize(IApplicationEnvironment environment, XElement settings)
        {
            _environment = environment;
            _connectionString = settings.Element("DataConnectionString").Value;

            Log = new CloudLogWriter(CloudStorage.ForAzureConnectionString(_connectionString).BuildBlobStorage());
            
            var autofacXml = settings.Element("AutofacAppConfig");
            _autofacConfig = autofacXml != null && !string.IsNullOrEmpty(autofacXml.Value)
                ? Convert.FromBase64String(autofacXml.Value)
                : null;
        }

        public CloudLogWriter Log { get; private set; }

        public ICloudRuntimeObserver CreateRuntimeObserverOptional()
        {
            return Observers.CreateRuntimeObserver(Log);
        }

        public List<CloudService> CreateServices(IRuntimeFinalizer finalizer)
        {
            var applicationBuilder = new ContainerBuilder();

            applicationBuilder.RegisterInstance(_environment);
            applicationBuilder.RegisterInstance(finalizer).As<IRuntimeFinalizer>();

            applicationBuilder.RegisterModule(new StorageModule(CloudStorageAccount.Parse(_connectionString)));
            applicationBuilder.RegisterModule(new DiagnosticsModule());

            applicationBuilder.RegisterType<Jobs.JobManager>();
            applicationBuilder.RegisterType<RuntimeFinalizer>().As<IRuntimeFinalizer>().InstancePerLifetimeScope();

            // Provisioning Observer Subject
            applicationBuilder.Register(c => new CloudProvisioningInstrumentationSubject(c.Resolve<IEnumerable<IObserver<ICloudProvisioningEvent>>>().ToArray()))
                .As<ICloudProvisioningObserver, IObservable<ICloudProvisioningEvent>>()
                .SingleInstance();

            // Load Application IoC Configuration and apply it to the builder
            if (_autofacConfig != null && _autofacConfig.Length > 0)
            {
                // HACK: need to copy settings locally first
                // HACK: hard-code string for local storage name
                const string fileName = "lokad.cloud.clientapp.config";
                const string resourceName = "LokadCloudStorage";

                var pathToFile = Path.Combine(_environment.GetLocalResourcePath(resourceName), fileName);
                File.WriteAllBytes(pathToFile, _autofacConfig);
                applicationBuilder.RegisterModule(new ConfigurationSettingsReader("autofac", pathToFile));
            }

            // Look for all cloud services currently loaded in the AppDomain
            var serviceTypes = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetExportedTypes()).SelectMany(x => x)
                .Where(t => t.IsSubclassOf(typeof(CloudService)) && !t.IsAbstract && !t.IsGenericType)
                .ToList();

            // Register the cloud services in the IoC Builder so we can support dependencies
            foreach (var type in serviceTypes)
            {
                applicationBuilder.RegisterType(type)
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

            var applicationContainer = applicationBuilder.Build();
            _disposable = applicationContainer;

            // Instanciate and return all the cloud services
            return serviceTypes.Select(type => (CloudService)applicationContainer.Resolve(type)).ToList();
        }

        public void Dispose()
        {
            var disposable = _disposable;
            if (disposable != null)
            {
                disposable.Dispose();
                _disposable = null;
            }
        }
    }
}
