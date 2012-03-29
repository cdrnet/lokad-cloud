#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Autofac;
using Lokad.Cloud.Storage.Instrumentation;
using Microsoft.WindowsAzure;

namespace Lokad.Cloud.Storage.Azure
{
    /// <summary>IoC module that registers
    /// <see cref="BlobStorageProvider"/>, <see cref="QueueStorageProvider"/> and
    /// <see cref="TableStorageProvider"/> from the <see cref="ICloudConfigurationSettings"/>.</summary>
    public sealed class StorageModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(StorageAccountFromSettings);
            builder.RegisterType<CloudFormatter>().As<IDataSerializer>();

            builder.Register(BlobStorageProvider);
            builder.Register(QueueStorageProvider).OnRelease(queues => queues.AbandonAll());
            builder.Register(TableStorageProvider);

            builder.Register(CloudStorageProviders).OnRelease(storage => storage.QueueStorage.AbandonAll());

            // Storage Observer Subject
            builder.Register(StorageObserver)
                .As<IStorageObserver, IObservable<IStorageEvent>>()
                .SingleInstance();
        }

        private static CloudStorageAccount StorageAccountFromSettings(IComponentContext c)
        {
            var settings = c.Resolve<ICloudConfigurationSettings>();
            CloudStorageAccount account;
            if (CloudStorageAccount.TryParse(settings.DataConnectionString, out account))
            {
                // http://blogs.msdn.com/b/windowsazurestorage/archive/2010/06/25/nagle-s-algorithm-is-not-friendly-towards-small-requests.aspx
                ServicePointManager.FindServicePoint(account.BlobEndpoint).UseNagleAlgorithm = false;
                ServicePointManager.FindServicePoint(account.TableEndpoint).UseNagleAlgorithm = false;
                ServicePointManager.FindServicePoint(account.QueueEndpoint).UseNagleAlgorithm = false;

                return account;
            }
            throw new InvalidOperationException("Failed to get valid connection string");
        }

        static CloudStorageProviders CloudStorageProviders(IComponentContext c)
        {
            return CloudStorage
                .ForAzureAccount(c.Resolve<CloudStorageAccount>())
                .WithDataSerializer(c.Resolve<IDataSerializer>())
                .WithObserver(c.Resolve<IStorageObserver>())
                .BuildStorageProviders();
        }

        static ITableStorageProvider TableStorageProvider(IComponentContext c)
        {
            return CloudStorage
                .ForAzureAccount(c.Resolve<CloudStorageAccount>())
                .WithDataSerializer(c.Resolve<IDataSerializer>())
                .WithObserver(c.Resolve<IStorageObserver>())
                .BuildTableStorage();
        }

        static IQueueStorageProvider QueueStorageProvider(IComponentContext c)
        {
            return CloudStorage
                .ForAzureAccount(c.Resolve<CloudStorageAccount>())
                .WithDataSerializer(c.Resolve<IDataSerializer>())
                .WithObserver(c.Resolve<IStorageObserver>())
                .BuildQueueStorage();
        }

        static IBlobStorageProvider BlobStorageProvider(IComponentContext c)
        {
            return CloudStorage
                .ForAzureAccount(c.Resolve<CloudStorageAccount>())
                .WithDataSerializer(c.Resolve<IDataSerializer>())
                .WithObserver(c.Resolve<IStorageObserver>())
                .BuildBlobStorage();
        }

        static StorageObserverSubject StorageObserver(IComponentContext c)
        {
            // will include any registered storage event observers, if there are any, as fixed subscriptions
            return new StorageObserverSubject(c.Resolve<IEnumerable<IObserver<IStorageEvent>>>().ToArray());
        }
    }
}