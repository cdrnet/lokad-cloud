#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Collections.Generic;
using System.Xml.Linq;
using Autofac;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.Instrumentation;
using Lokad.Cloud.ServiceFabric;

namespace Lokad.Cloud.EntryPoint
{
    public interface ICloudFactory
    {
        void Initialize(IApplicationEnvironment environment, XElement settings);

        CloudLogWriter Log { get; }

        ICloudRuntimeObserver CreateRuntimeObserverOptional();

        List<CloudService> CreateServices(IRuntimeFinalizer finalizer);
    }
}