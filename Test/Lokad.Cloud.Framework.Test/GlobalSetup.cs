#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Autofac;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.Autofac;
using Lokad.Cloud.Stubs;
using Microsoft.WindowsAzure;

namespace Lokad.Cloud.Test
{
    public static class GlobalSetup
    {
        static IContainer _container;

        static IContainer Setup()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule<AzureModule>();
            builder.RegisterInstance(CloudStorageAccount.DevelopmentStorageAccount);
            builder.RegisterType<StubEnvironment>().As<IApplicationEnvironment>().InstancePerLifetimeScope();

            return builder.Build();
        }

        /// <summary>Gets the IoC container as initialized by the setup.</summary>
        public static IContainer Container 
        { 
            get { return _container ?? (_container = Setup()); }
        }
    }
}