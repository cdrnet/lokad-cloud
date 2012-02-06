#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.Jobs;
using Lokad.Cloud.ServiceFabric;
using Lokad.Cloud.Instrumentation;
using Lokad.Cloud.Services.EntryPoint.Diagnostics;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.Services.EntryPoint
{
    /// <summary>
    /// Simple Entry Point, without any need for IoC.
    /// </summary>
    public class SimpleApplicationEntryPoint : IApplicationEntryPoint
    {
        private readonly SimpleRunStrategy _strategy = new SimpleRunStrategy();

        public void Run(XElement settings, IDeploymentReader deploymentReader, IApplicationEnvironment environment, CancellationToken cancellationToken)
        {
            var dataConnectionString = settings.Element("DataConnectionString").Value;
            var log = new CloudLogWriter(CloudStorage.ForAzureConnectionString(dataConnectionString).BuildBlobStorage());
            var applicationObserver = (IApplicationObserver)SimpleLoggingObservers.CreateForApplication(log);
            var runtimeObserver = (IRuntimeObserver)SimpleLoggingObservers.CreateForRuntime(log);
            var cloudEnvironment = new EnvironmentAdapter(environment);

            _strategy.RunInfinitely(cloudEnvironment, cancellationToken, runOnce =>
                {
                    var jobs = new JobManager(runtimeObserver);
                    var storage = CloudStorage
                        .ForAzureConnectionString(dataConnectionString)
                        .WithObserver(SimpleLoggingObservers.CreateForStorage(log))
                        .BuildStorageProviders();

                    var services = AppDomain.CurrentDomain.GetAssemblies()
                        .Select(a => a.GetExportedTypes()).SelectMany(x => x)
                        .Where(t => t.IsSubclassOf(typeof(CloudService)) && !t.IsAbstract && !t.IsGenericType)
                        .Select(t =>
                        {
                            var service = (CloudService)Activator.CreateInstance(t);

                            service.Storage = storage;
                            service.Environment = cloudEnvironment;
                            service.Log = log;
                            service.Observer = applicationObserver;
                            service.RuntimeObserver = runtimeObserver;
                            service.Jobs = jobs;

                            Inject(service);

                            service.Initialize();

                            return service;
                        })
                        .ToList();

                    try
                    {
                        runOnce(runtimeObserver, services);
                    }
                    finally
                    {
                        foreach (var disposable in services.OfType<IDisposable>())
                        {
                            disposable.Dispose();
                        }

                        storage.QueueStorage.AbandonAll();
                    }
                });
        }

        public void OnSettingsChanged(XElement settings)
        {
            _strategy.OnSettingsChanged();
        }

        /// <summary>
        /// Override this method to inject additional objects into your services,
        /// before they are initialized.
        /// </summary>
        protected virtual void Inject(CloudService service)
        {
        }
    }
}
