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
using Lokad.Cloud.ServiceFabric;
using Lokad.Cloud.Instrumentation;
using Lokad.Cloud.Storage;
using Lokad.Cloud.Storage.Instrumentation;

namespace Lokad.Cloud.EntryPoint
{
    /// <summary>
    /// Simple Entry Point, without any need for IoC.
    /// </summary>
    public class ApplicationEntryPoint : IApplicationEntryPoint
    {
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
            var jobs = new Jobs.JobManager(Log);
            var finalizer = new RuntimeFinalizer();
            var storage = CloudStorage
                .ForAzureConnectionString(DataConnectionString)
                .WithObserver(CreateOptionalStorageObserver())
                .WithRuntimeFinalizer(finalizer)
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
                        service.Jobs = jobs;

                        Inject(service);

                        service.Initialize();

                        return service;
                    })
                .ToList();

            var runner = new CloudServiceRunner(Log, CreateOptionalRuntimeObserver());
            try
            {
                runner.Run(environment, services, cancellationToken);
            }
            finally
            {
                finalizer.FinalizeRuntime();
            }
        }

        public void OnSettingsChanged(XElement settings)
        {
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
        /// <param name="service"></param>
        protected virtual void Inject(CloudService service)
        {
        }
    }
}
