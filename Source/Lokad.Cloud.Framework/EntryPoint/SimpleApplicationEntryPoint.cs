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
using Lokad.Cloud.Storage;
using Lokad.Cloud.Storage.Instrumentation;

namespace Lokad.Cloud.EntryPoint
{
    /// <summary>
    /// Simple Entry Point, without any need for IoC.
    /// </summary>
    public class SimpleApplicationEntryPoint : IApplicationEntryPoint
    {
        private CancellationTokenSource _cancelledOrSettingsChangedCts;

        protected IApplicationEnvironment Environment { get; private set; }
        protected XElement Settings { get; private set; }
        protected string DataConnectionString { get; set; }

        protected ILog Log { get; private set; }

        public void Run(XElement settings, IDeploymentReader deploymentReader, IApplicationEnvironment environment, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            Environment = environment;
            Settings = settings;
            DataConnectionString = Settings.Element("DataConnectionString").Value;

            Log = CreateLog();
            var applicationObserver = CreateOptionalApplicationObserver();
            var runtimeObserver = CreateOptionalRuntimeObserver();

            // We want to recycle if the settings change, but not actually unload the AppDomain.
            // That's why we loop here instead of just return back to the caller.
            // This is purely custom behavior, change in your EntryPoint as you see fit.
            while (!cancellationToken.IsCancellationRequested)
            {
                _cancelledOrSettingsChangedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                var jobs = new JobManager(runtimeObserver);
                var storage = CloudStorage
                    .ForAzureConnectionString(DataConnectionString)
                    .WithObserver(CreateOptionalStorageObserver())
                    .BuildStorageProviders();

                var services = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetExportedTypes()).SelectMany(x => x)
                    .Where(t => t.IsSubclassOf(typeof(CloudService)) && !t.IsAbstract && !t.IsGenericType)
                    .Select(t =>
                        {
                            var service = (CloudService)Activator.CreateInstance(t);

                            service.Storage = storage;
                            service.Environment = Environment;
                            service.Log = Log;
                            service.Observer = applicationObserver;
                            service.RuntimeObserver = runtimeObserver;
                            service.Jobs = jobs;

                            Inject(service);

                            service.Initialize();

                            return service;
                        })
                    .ToList();

                var runner = new CloudServiceRunner(runtimeObserver);
                try
                {
                    runner.Run(environment, services, _cancelledOrSettingsChangedCts.Token);
                }
                finally
                {
                    foreach(var disposable in services.OfType<IDisposable>())
                    {
                        disposable.Dispose();
                    }

                    storage.QueueStorage.AbandonAll();
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

        protected virtual ILog CreateLog()
        {
            return new CloudLogWriter(CloudStorage.ForAzureConnectionString(DataConnectionString).BuildBlobStorage());
        }

        protected virtual IStorageObserver CreateOptionalStorageObserver()
        {
            return SimpleLoggingObservers.CreateForStorage(Log);
        }

        protected virtual IRuntimeObserver CreateOptionalRuntimeObserver()
        {
            return SimpleLoggingObservers.CreateForRuntime(Log);
        }

        protected virtual IApplicationObserver CreateOptionalApplicationObserver()
        {
            return SimpleLoggingObservers.CreateForApplication(Log);
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
