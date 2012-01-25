#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.ServiceFabric;
using Lokad.Cloud.Instrumentation;
using Lokad.Cloud.Storage;

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

            Configure();

            Log = CreateLog();

            var runner = new CloudServiceRunner();

            runner.Run(
                environment,
                CreateServices,
                DisposeServices,
                Log,
                CreateRuntimeObserverOptional(),
                cancellationToken);
        }

        public void OnSettingsChanged(XElement settings)
        {
        }

        protected virtual void Configure()
        {
            DataConnectionString = Settings.Element("DataConnectionString").Value;
        }

        protected virtual ILog CreateLog()
        {
            return new CloudLogWriter(CloudStorage.ForAzureConnectionString(DataConnectionString).BuildBlobStorage());
        }

        protected virtual IRuntimeObserver CreateRuntimeObserverOptional()
        {
            return Observers.CreateRuntimeObserver(Log);
        }

        protected virtual List<CloudService> CreateServices(IRuntimeFinalizer finalizer)
        {
            var storage = CloudStorage
                .ForAzureConnectionString(DataConnectionString)
                .WithObserver(Observers.CreateStorageObserver(Log))
                .WithRuntimeFinalizer(finalizer)
                .BuildStorageProviders();

            var jobs = new Jobs.JobManager(Log);

            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetExportedTypes()).SelectMany(x => x)
                .Where(t => t.IsSubclassOf(typeof(CloudService)) && !t.IsAbstract && !t.IsGenericType)
                .Select(t =>
                {
                    var service = (CloudService)Activator.CreateInstance(t);

                    service.Storage = storage;
                    service.Environment = Environment;
                    service.Log = Log;
                    service.Jobs = jobs;

                    Inject(service);

                    service.Initialize();

                    return service;
                })
                .ToList();
        }

        protected virtual void DisposeServices()
        {
        }

        protected virtual void Inject(CloudService service)
        {
        }
    }
}
