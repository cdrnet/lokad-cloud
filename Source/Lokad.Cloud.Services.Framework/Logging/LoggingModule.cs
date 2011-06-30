#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Autofac;
using Lokad.Cloud.Storage;
using Microsoft.WindowsAzure;

namespace Lokad.Cloud.Services.Framework.Logging
{
    /// <summary>Cloud Logging IoC Module.</summary>
    public class LoggingModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(CloudLogWriter).As<ILogWriter>();
        }

        static CloudLogWriter CloudLogWriter(IComponentContext c)
        {
            return new CloudLogWriter(BlobStorageForDiagnostics(c));
        }

        static IBlobStorageProvider BlobStorageForDiagnostics(IComponentContext c)
        {
            // Neither log nor observers are provided since the providers
            // used for logging obviously can't log themselves (cyclic dependency)

            // We also always use the CloudFormatter, so this is equivalent
            // to the RuntimeProvider, for the same reasons.

            return CloudStorage
                .ForAzureAccount(c.Resolve<CloudStorageAccount>())
                .WithDataSerializer(new CloudFormatter())
                .BuildBlobStorage();
        }
    }
}
