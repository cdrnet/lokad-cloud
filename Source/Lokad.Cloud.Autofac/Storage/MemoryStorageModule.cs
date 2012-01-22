#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Autofac;
using Lokad.Cloud.Storage;
using Lokad.Cloud.Storage.InMemory;
using Lokad.Cloud.Storage.Instrumentation;

namespace Lokad.Cloud.Autofac.Storage
{
    /// <summary>
    /// IoC Module that provides storage providers linked to in-memory only storage:
    /// - CloudStorageProviders
    /// - IBlobStorageProvider
    /// - IQueueStorageProvider
    /// - ITableStorageProvider
    /// </summary>
    public sealed class MemoryStorageModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(c => CloudStorage
                .ForInMemoryStorage()
                .WithDataSerializer(c.Resolve<IDataSerializer>())
                .WithObserver(c.ResolveOptional<ICloudStorageObserver>())
                .WithRuntimeFinalizer(c.ResolveOptional<IRuntimeFinalizer>())
                .BuildStorageProviders());

            builder.Register(c => CloudStorage
                .ForInMemoryStorage()
                .WithDataSerializer(c.Resolve<IDataSerializer>())
                .WithObserver(c.ResolveOptional<ICloudStorageObserver>())
                .WithRuntimeFinalizer(c.ResolveOptional<IRuntimeFinalizer>())
                .BuildBlobStorage());

            builder.Register(c => CloudStorage
                .ForInMemoryStorage()
                .WithDataSerializer(c.Resolve<IDataSerializer>())
                .WithObserver(c.ResolveOptional<ICloudStorageObserver>())
                .WithRuntimeFinalizer(c.ResolveOptional<IRuntimeFinalizer>())
                .BuildQueueStorage());

            builder.Register(c => CloudStorage
                .ForInMemoryStorage()
                .WithDataSerializer(c.Resolve<IDataSerializer>())
                .WithObserver(c.ResolveOptional<ICloudStorageObserver>())
                .WithRuntimeFinalizer(c.ResolveOptional<IRuntimeFinalizer>())
                .BuildTableStorage());

            builder.Register(c => new Diagnostics.NeutralLogStorage { BlobStorage = new MemoryBlobStorageProvider() });
        }
    }
}
