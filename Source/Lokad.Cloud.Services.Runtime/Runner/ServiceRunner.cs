#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Lokad.Cloud.Services.Framework;
using Lokad.Cloud.Services.Runtime.Settings;
using Lokad.Cloud.Services.Runtime.WorkingSet;

namespace Lokad.Cloud.Services.Runtime.Runner
{
    /// <summary>
    /// A runtime cell is runtime unit that runs cloud services assigned to that cell
    /// until something unexpected happens. Multiple distinct cells can be active in
    /// parallel. A cell is assumed to be running in its own thread, or in its own
    /// long-running task. Cells can also be used in standalone scenarios e.g. for
    /// unit or integration testing or for off-line simulations.
    /// </summary>
    /// <remarks>
    /// A cell runner does neither check for new package assemblies, configuration
    /// or server settings nor supports applying such changes. Instead, cancel
    /// the runner and start it with the new service instances.
    /// </remarks>
    internal class ServiceRunner
    {
        // TODO: Make this publicly available (useful for testing)

        private static readonly TimeSpan IdleTime = TimeSpan.FromSeconds(10);

        /// <param name="services">List of all enabled cloud services, fully populated (e.g. using IoC) but not initialized yet.</param>
        /// <remarks> Only returns on unhandled exceptions or when canceled.</remarks>
        internal void Run(List<ICloudService> services, CellServiceSettings settings, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            // 1. INITIALIZE SERVICES

            foreach (var service in services)
            {
                service.Initialize();
            }

            var queuedCloudServiceRunner = new QueuedCloudServiceRunner(Match(services.OfType<UntypedQueuedCloudService>(), settings.QueuedCloudServices));
            var scheduledWorkerServiceRunner = new ScheduledWorkerServiceRunner(Match(services.OfType<ScheduledWorkerService>(), settings.ScheduledWorkerServices));
            var scheduledCloudServiceRunner = new ScheduledCloudServiceRunner(Match(services.OfType<ScheduledCloudService>(), settings.ScheduledCloudServices));
            var daemonServices = new List<DaemonService>(services.OfType<DaemonService>());

            // 2. LOCAL CANCELLATION SUPPORT

            var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var localCancellationToken = cancellationTokenSource.Token;

            // 3. START DAEMONS, REGISTER FOR UNLOAD

            if (daemonServices.Count != 0)
            {
                foreach (var service in daemonServices)
                {
                    service.Initialize();
                    service.OnStart();
                }

                localCancellationToken.Register(() =>
                    {
                        foreach (var service in daemonServices)
                        {
                            service.OnStop();
                        }
                    });
            }

            // 4. RUN REGULAR SERVICES IN MAIN LOOP

            try
            {
                while (!localCancellationToken.IsCancellationRequested)
                {
                    var hadWork = false;
                    hadWork |= queuedCloudServiceRunner.RunSingle(localCancellationToken);
                    hadWork |= scheduledWorkerServiceRunner.RunSingle(localCancellationToken);
                    hadWork |= scheduledCloudServiceRunner.RunSingle(localCancellationToken);

                    if (!hadWork)
                    {
                        localCancellationToken.WaitHandle.WaitOne(IdleTime);
                    }
                }
            }
            finally
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
            }
        }

        private static IEnumerable<ServiceWithSettings<TService, TSetting>> Match<TService, TSetting>(IEnumerable<TService> services, TSetting[] settings)
            where TService : ICloudService
            where TSetting : CommonServiceSettings
        {
            var settingsByType = settings.ToDictionary(s => s.TypeName);

            return services
                .Select(service => new { Service = service, Settings = settingsByType[service.GetType().FullName] })
                .Where(s => !s.Settings.IsDisabled)
                .Select(s => new ServiceWithSettings<TService, TSetting>(s.Service, s.Settings));
        }
    }
}
