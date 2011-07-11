#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Lokad.Cloud.Services.Framework;
using Lokad.Cloud.Services.Management.Settings;
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

            var queuedCloudServiceRunner = new QueuedCloudServiceRunner(MatchActive(services.OfType<UntypedQueuedCloudService>(), settings.QueuedCloudServices, settings.CellName));
            var scheduledWorkerServiceRunner = new ScheduledWorkerServiceRunner(MatchActive(services.OfType<ScheduledWorkerService>(), settings.ScheduledWorkerServices, settings.CellName));
            var scheduledCloudServiceRunner = new ScheduledCloudServiceRunner(MatchActive(services.OfType<ScheduledCloudService>(), settings.ScheduledCloudServices, settings.CellName));
            var daemonServices = new List<ServiceWithSettings<DaemonService, DaemonServiceSettings>>(MatchActive(services.OfType<DaemonService>(), settings.DaemonServices, settings.CellName));

            // 2. LOCAL CANCELLATION SUPPORT

            var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var localCancellationToken = cancellationTokenSource.Token;

            // 3. START DAEMONS, REGISTER FOR UNLOAD

            if (daemonServices.Count != 0)
            {
                foreach (var service in daemonServices)
                {
                    service.Service.Initialize();
                    service.Service.OnStart();
                }

                localCancellationToken.Register(() =>
                    {
                        foreach (var service in daemonServices)
                        {
                            service.Service.OnStop();
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

        /// <summary>
        /// Matches services with their settings but skip disabled services (globally or for this cell only).
        /// </summary>
        private static IEnumerable<ServiceWithSettings<TService, TSetting>> MatchActive<TService, TSetting>(IEnumerable<TService> services, IEnumerable<TSetting> settings, string cellName)
            where TService : ICloudService
            where TSetting : CommonServiceSettings
        {
            var settingsByType = settings.ToDictionary(s => s.TypeName);
            foreach(var service in services)
            {
                TSetting setting;
                if (settingsByType.TryGetValue(service.GetType().FullName, out setting) && !setting.IsDisabled && setting.CellAffinity.Contains(cellName))
                {
                    yield return new ServiceWithSettings<TService, TSetting>(service, setting);
                }
            }
        }
    }
}
