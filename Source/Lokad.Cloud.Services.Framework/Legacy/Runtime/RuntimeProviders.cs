#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.Services.Framework.Logging;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.Runtime
{
    /// <remarks>
    /// The purpose of having a separate class mirroring CloudStorageProviders
    /// just for runtime classes is to make it easier to distinct them and avoid
    /// mixing them up (also simplifying IoC on the way), since they have a different
    /// purpose and are likely configured slightly different. E.g. Runtime Providers
    /// have a fixed data serializer, while the application can choose it's serializer
    /// for it's own application code.
    /// </remarks>
    public class RuntimeProviders
    {
        /// <summary>Abstracts the Blob Storage.</summary>
        public IBlobStorageProvider BlobStorage { get; private set; }

        /// <summary>Abstracts the Queue Storage.</summary>
        public IQueueStorageProvider QueueStorage { get; private set; }

        /// <summary>Abstracts the Table Storage.</summary>
        public ITableStorageProvider TableStorage { get; private set; }

        /// <summary>Abstracts the finalizer (used for fast resource release
        /// in case of runtime shutdown).</summary>
        public IRuntimeFinalizer RuntimeFinalizer { get; private set; }

        public ILogWriter Log { get; private set; }

        /// <summary>IoC constructor.</summary>
        public RuntimeProviders(
            IBlobStorageProvider blobStorage,
            IQueueStorageProvider queueStorage,
            ITableStorageProvider tableStorage,
            IRuntimeFinalizer runtimeFinalizer,
            ILogWriter log)
        {
            BlobStorage = blobStorage;
            QueueStorage = queueStorage;
            TableStorage = tableStorage;
            RuntimeFinalizer = runtimeFinalizer;
            Log = log;
        }
    }
}