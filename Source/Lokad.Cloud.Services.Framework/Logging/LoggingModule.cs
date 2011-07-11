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
            return new CloudLogWriter(CloudStorage
                .ForAzureAccount(c.Resolve<CloudStorageAccount>())
                .BuildStorageProviders());
        }
    }
}
