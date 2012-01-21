#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.Instrumentation;
using Lokad.Cloud.ServiceFabric;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.EntryPoint
{
    /// <summary>
    /// Simple yet extensible cloud factory without any IoC
    /// </summary>
    public class SimpleCloudFactory : ICloudFactory
    {
        protected IApplicationEnvironment Environment { get; private set; }
        protected string ConnectionString { get; set; }

        public virtual void Initialize(IApplicationEnvironment environment, XElement settings)
        {
            Environment = environment;
            ConnectionString = settings.Element("DataConnectionString").Value;

            Log = new CloudLogWriter(CloudStorage.ForAzureConnectionString(ConnectionString).BuildBlobStorage());
        }

        public CloudLogWriter Log { get; protected set; }

        public virtual ICloudRuntimeObserver CreateRuntimeObserverOptional()
        {
            return Observers.CreateRuntimeObserver(Log);
        }

        public virtual List<CloudService> CreateServices(IRuntimeFinalizer finalizer)
        {
            var storage = CloudStorage
                .ForAzureConnectionString(ConnectionString)
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

        protected virtual void Inject(CloudService service)
        {
        }
    }
}
