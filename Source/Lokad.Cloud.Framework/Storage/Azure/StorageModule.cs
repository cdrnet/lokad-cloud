#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Autofac;
using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.Runtime;
using Lokad.Cloud.Storage.Instrumentation;
using Lokad.Cloud.Storage.Instrumentation.Events;
using Microsoft.WindowsAzure;

namespace Lokad.Cloud.Storage.Azure
{
    /// <summary>
    /// IoC module that registers <see cref="BlobStorageProvider"/>,
    /// <see cref="QueueStorageProvider"/> and <see cref="TableStorageProvider"/>.
    /// </summary>
    public sealed class StorageModule : Module
    {
        private readonly CloudStorageAccount _storageAccount;

        public StorageModule(CloudStorageAccount storageAccount)
        {
            _storageAccount = storageAccount;

            ServicePointManager.FindServicePoint(storageAccount.BlobEndpoint).UseNagleAlgorithm = false;
            ServicePointManager.FindServicePoint(storageAccount.TableEndpoint).UseNagleAlgorithm = false;
            ServicePointManager.FindServicePoint(storageAccount.QueueEndpoint).UseNagleAlgorithm = false;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_storageAccount);
            builder.RegisterType<CloudFormatter>().As<IDataSerializer>();

            builder.Register(BlobStorageProvider);
            builder.Register(QueueStorageProvider);
            builder.Register(TableStorageProvider);

            builder.Register(RuntimeProviders);
            builder.Register(CloudStorageProviders);

            // Storage Observer Subject
            builder.Register(StorageObserver)
                .As<ICloudStorageObserver, IObservable<ICloudStorageEvent>>()
                .SingleInstance();
        }

        RuntimeProviders RuntimeProviders(IComponentContext c)
        {
            return CloudStorage
                .ForAzureAccount(_storageAccount)
                .WithObserver(c.Resolve<ICloudStorageObserver>())
                .WithRuntimeFinalizer(c.ResolveOptional<IRuntimeFinalizer>())
                .BuildRuntimeProviders(c.ResolveOptional<ILog>());
        }

        CloudStorageProviders CloudStorageProviders(IComponentContext c)
        {
            return CloudStorage
                .ForAzureAccount(_storageAccount)
                .WithDataSerializer(c.Resolve<IDataSerializer>())
                .WithObserver(c.Resolve<ICloudStorageObserver>())
                .WithRuntimeFinalizer(c.ResolveOptional<IRuntimeFinalizer>())
                .BuildStorageProviders();
        }

        ITableStorageProvider TableStorageProvider(IComponentContext c)
        {
            return CloudStorage
                .ForAzureAccount(_storageAccount)
                .WithDataSerializer(c.Resolve<IDataSerializer>())
                .WithObserver(c.Resolve<ICloudStorageObserver>())
                .WithRuntimeFinalizer(c.ResolveOptional<IRuntimeFinalizer>())
                .BuildTableStorage();
        }

        IQueueStorageProvider QueueStorageProvider(IComponentContext c)
        {
            return CloudStorage
                .ForAzureAccount(_storageAccount)
                .WithDataSerializer(c.Resolve<IDataSerializer>())
                .WithObserver(c.Resolve<ICloudStorageObserver>())
                .WithRuntimeFinalizer(c.ResolveOptional<IRuntimeFinalizer>())
                .BuildQueueStorage();
        }

        IBlobStorageProvider BlobStorageProvider(IComponentContext c)
        {
            return CloudStorage
                .ForAzureAccount(_storageAccount)
                .WithDataSerializer(c.Resolve<IDataSerializer>())
                .WithObserver(c.Resolve<ICloudStorageObserver>())
                .WithRuntimeFinalizer(c.ResolveOptional<IRuntimeFinalizer>())
                .BuildBlobStorage();
        }

        static CloudStorageInstrumentationSubject StorageObserver(IComponentContext c)
        {
            // will include any registered storage event observers, if there are any, as fixed subscriptions
            return new CloudStorageInstrumentationSubject(c.Resolve<IEnumerable<IObserver<ICloudStorageEvent>>>().ToArray());
        }
    }
}