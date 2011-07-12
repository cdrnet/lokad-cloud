#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Threading;
using Lokad.Cloud.Services.Framework;
using Lokad.Cloud.Services.Management.Settings;
using Lokad.Cloud.Services.Runtime.Runner;

namespace Lokad.Cloud.Services.Runtime.WorkingSet
{
    /// <summary>
    /// AppDomain Entry Point for the cell process (single use).
    /// </summary>
    internal sealed class CellProcessAppDomainEntryPoint : MarshalByRefObject
    {
        private readonly CancellationTokenSource _externalCancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent _completedWaitHandle = new ManualResetEvent(false);

        // NOTE: cancellation tokens and wait handles cannot pass AppDomain borders

        /// <remarks>Never run a cell process entry point more than once per AppDomain.</remarks>
        public void Run(byte[] packageAssemblies, byte[] packageConfig, CloudServicesSettings servicesSettings)
        {
            try
            {
                using (var container = new ServiceContainer(packageAssemblies, packageConfig))
                {
                    while (!_externalCancellationTokenSource.Token.IsCancellationRequested)
                    {
                        var runner = new ServiceRunner();
                        runner.Run(
                            container.ResolveServices<UntypedQueuedCloudService, QueuedCloudServiceSettings>(servicesSettings.QueuedCloudServices),
                            container.ResolveServices<ScheduledCloudService, ScheduledCloudServiceSettings>(servicesSettings.ScheduledCloudServices),
                            container.ResolveServices<ScheduledWorkerService, ScheduledWorkerServiceSettings>(servicesSettings.ScheduledWorkerServices),
                            container.ResolveServices<DaemonService, DaemonServiceSettings>(servicesSettings.DaemonServices),
                            _externalCancellationTokenSource.Token);
                    }
                }
            }
            catch (ThreadAbortException)
            {
                Thread.ResetAbort();
            }
            finally
            {
                _completedWaitHandle.Set();
            }
        }

        public void Cancel()
        {
            _externalCancellationTokenSource.Cancel();
        }

        public void ShutdownWait()
        {
            // TODO: Consider a timeout
            _externalCancellationTokenSource.Cancel();
            _completedWaitHandle.WaitOne();
        }
    }
}