#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Autofac;
using Lokad.Cloud.Mock;

namespace Lokad.Cloud.Test
{
    public sealed class GlobalSetup
    {
        static IContainer _container;

        static IContainer Setup()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new CloudModule());
            builder.RegisterInstance(new CloudConfigurationSettings { DataConnectionString = "UseDevelopmentStorage=true" });

            builder.RegisterType<MockEnvironment>().As<IEnvironment>().SingleInstance();

            return builder.Build();
        }

        /// <summary>Gets the IoC container as initialized by the setup.</summary>
        public static IContainer Container 
        { 
            get { return _container ?? (_container = Setup()); }
        }
    }
}