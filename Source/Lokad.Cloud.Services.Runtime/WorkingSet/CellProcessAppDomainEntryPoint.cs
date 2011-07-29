#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Lokad.Cloud.Services.Framework;
using Lokad.Cloud.Services.Management.Application;
using Lokad.Cloud.Services.Runtime.Internal;
using Lokad.Cloud.Services.Runtime.Runner;

namespace Lokad.Cloud.Services.Runtime.WorkingSet
{
    /// <summary>
    /// AppDomain Entry Point for the cell process (single use).
    /// </summary>
    internal sealed class CellProcessAppDomainEntryPoint : MarshalByRefObject
    {
        private readonly CancellationTokenSource _externalCancellationTokenSource = new CancellationTokenSource();

        /// <remarks>Never run a cell process entry point more than once per AppDomain.</remarks>
        public void Run(byte[] packageAssemblies, byte[] packageConfig, string servicesSettingsXml, RuntimeCellEnvironment environment)
        {
            // Load Application Assemblies into the AppDomain
            var reader = new CloudApplicationPackageReader();
            var package = reader.ReadPackage(packageAssemblies, false);
            package.LoadAssemblies();

            // Build IoC container, resolve all cloud services and run them.
            var servicesSettings = XElement.Parse(servicesSettingsXml).Elements("Service").ToLookup(service => service.AttributeValue("type"));
            using (var container = new ServiceContainer(packageConfig, environment))
            {
                while (!_externalCancellationTokenSource.Token.IsCancellationRequested)
                {
                    var runner = new ServiceRunner();
                    runner.Run(
                        container.ResolveServices<UntypedQueuedCloudService>(servicesSettings["QueuedCloudService"]),
                        container.ResolveServices<ScheduledCloudService>(servicesSettings["ScheduledCloudService"]),
                        container.ResolveServices<ScheduledWorkerService>(servicesSettings["ScheduledWorkerService"]),
                        container.ResolveServices<DaemonService>(servicesSettings["DaemonService"]),
                        _externalCancellationTokenSource.Token);
                }
            }
        }

        public void Cancel()
        {
            _externalCancellationTokenSource.Cancel();
        }

        public void Reconfigure(byte[] newPackageConfig)
        {
            // reconfigure means that we have new IoC modules to load, hence we need to rebuild the service container.

            throw new NotImplementedException();
        }

        public void ApplySettings(string newServicesSettingsXml)
        {
            throw new NotImplementedException();
        }
    }
}