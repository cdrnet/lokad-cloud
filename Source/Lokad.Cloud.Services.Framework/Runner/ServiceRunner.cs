#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Lokad.Cloud.Services.Framework.Runner
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
    /// Also, a service runner does not care of any cell affinities, it simply
    /// runs each service which matching settings.
    /// </remarks>
    public class ServiceRunner
    {
        public static TimeSpan IdleTime = TimeSpan.FromSeconds(10);

        /// <remarks> Only returns on unhandled exceptions or when canceled.</remarks>
        public void Run(
            IEnumerable<ServiceWithSettings<UntypedQueuedCloudService>> queuedCloudServices,
            IEnumerable<ServiceWithSettings<ScheduledCloudService>> scheduledCloudServices,
            IEnumerable<ServiceWithSettings<ScheduledWorkerService>> scheduledWorkerServices,
            IEnumerable<ServiceWithSettings<DaemonService>> daemonServices,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            // 1. LOCAL CANCELLATION SUPPORT

            var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var localCancellationToken = cancellationTokenSource.Token;

            // 2. BUILD SERVICE RUNNERS FOR ENABLED SERVICES

            var queuedCloudServiceRunner = new QueuedCloudServiceRunner(queuedCloudServices.Where(s => s.Settings.AttributeValue("disabled") != "true").ToList());
            var scheduledCloudServiceRunner = new ScheduledCloudServiceRunner(scheduledCloudServices.Where(s => s.Settings.AttributeValue("disabled") != "true").ToList());
            var scheduledWorkerServiceRunner = new ScheduledWorkerServiceRunner(scheduledWorkerServices.Where(s => s.Settings.AttributeValue("disabled") != "true").ToList());
            var daemonServiceRunner = new DaemonServiceRunner(daemonServices.Where(s => s.Settings.AttributeValue("disabled") != "true").ToList());

            // 3. INITIALIZE SERVICES

            queuedCloudServiceRunner.Initialize();
            scheduledCloudServiceRunner.Initialize();
            scheduledWorkerServiceRunner.Initialize();
            daemonServiceRunner.Initialize();

            // 4. START SERVICES, REGISTER FOR STOP  (mostly for daemons, but other services can use it as well)

            queuedCloudServiceRunner.Start();
            scheduledCloudServiceRunner.Start();
            scheduledWorkerServiceRunner.Start();
            daemonServiceRunner.Start();

            localCancellationToken.Register(() =>
                {
                    queuedCloudServiceRunner.Stop();
                    scheduledCloudServiceRunner.Stop();
                    scheduledWorkerServiceRunner.Stop();
                    daemonServiceRunner.Stop();
                });

            // 5. RUN REGULAR SERVICES IN MAIN LOOP

            try
            {
                while (!localCancellationToken.IsCancellationRequested)
                {
                    var hadWork = false;
                    hadWork |= queuedCloudServiceRunner.RunSingle(localCancellationToken);
                    hadWork |= scheduledCloudServiceRunner.RunSingle(localCancellationToken);
                    hadWork |= scheduledWorkerServiceRunner.RunSingle(localCancellationToken);

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
    }
}
