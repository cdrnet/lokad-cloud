#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Cloud.Services.Framework.Instrumentation;
using Lokad.Cloud.Services.Framework.Instrumentation.Events;
using Lokad.Cloud.Services.Management.Settings;

namespace Lokad.Cloud.Services.Runtime.WorkingSet
{
    /// <summary>
    /// Cell process host a service runner for a single cell,
    /// isolated in its own AppDomain and in a separate thread.
    /// The cell runner will be automatically restarted in exceptional
    /// circumstances.
    /// </summary>
    internal sealed class CellProcess
    {
        private static readonly TimeSpan FloodFrequencyThreshold = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan DelayWhenFlooding = TimeSpan.FromMinutes(5);

        private readonly ICloudRuntimeObserver _observer;

        private readonly string _cellName;
        private readonly byte[] _packageAssemblies;
        private CloudServicesSettings _servicesSettings;
        private byte[] _packageConfig;

        private volatile CellProcessAppDomainEntryPoint _entryPoint;

        public CellProcess(
            byte[] packageAssemblies,
            byte[] packageConfig,
            string cellName,
            CloudServicesSettings servicesSettings,
            ICloudRuntimeObserver observer)
        {
            _packageAssemblies = packageAssemblies;
            _packageConfig = packageConfig;
            _cellName = cellName;
            _servicesSettings = servicesSettings;
            _observer = observer;
        }

        public Task Run(CancellationToken cancellationToken)
        {
            var completionSource = new TaskCompletionSource<object>(TaskCreationOptions.LongRunning);

            var thread = new Thread(() =>
                {
                    var currentRoundStartTime = DateTimeOffset.UtcNow - FloodFrequencyThreshold;
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var lastRoundStartTime = currentRoundStartTime;
                        currentRoundStartTime = DateTimeOffset.UtcNow;

                        AppDomain domain = AppDomain.CreateDomain("LokadCloudServiceRuntimeCell_" + _cellName, null, AppDomain.CurrentDomain.SetupInformation);
                        try
                        {
                            domain.UnhandledException += (sender, args) => _observer.TryNotify(() => new CloudRuntimeExceptionProcessRestartingEvent(args.ExceptionObject as Exception, _cellName, false));

                            try
                            {
                                _entryPoint = (CellProcessAppDomainEntryPoint)domain.CreateInstanceAndUnwrap(
                                    Assembly.GetExecutingAssembly().FullName, typeof(CellProcessAppDomainEntryPoint).FullName);
                            }
                            catch (Exception exception)
                            {
                                // Fatal Error
                                _observer.TryNotify(() => new CloudRuntimeFatalErrorProcessRestartEvent(exception, _cellName));
                                cancellationToken.WaitHandle.WaitOne(DelayWhenFlooding);
                                continue;
                            }

                            // Forward cancellation token to AppDomain-internal cancellation token source
                            var registration = cancellationToken.Register(_entryPoint.Cancel);
                            try
                            {
                                _observer.TryNotify(() => new CloudRuntimeCellStartedEvent(_cellName));
                                _entryPoint.Run(_packageAssemblies, _packageConfig, _servicesSettings);
                            }
                            catch (Exception exception)
                            {
                                _entryPoint = null;
                                if ((DateTimeOffset.UtcNow - lastRoundStartTime) < FloodFrequencyThreshold)
                                {
                                    _observer.TryNotify(() => new CloudRuntimeExceptionProcessRestartingEvent(exception, _cellName, true));
                                    cancellationToken.WaitHandle.WaitOne(DelayWhenFlooding);
                                }
                                else
                                {
                                    _observer.TryNotify(() => new CloudRuntimeExceptionProcessRestartingEvent(exception, _cellName, false));
                                }
                                continue;
                            }
                            finally
                            {
                                _entryPoint = null;
                                _observer.TryNotify(() => new CloudRuntimeCellStoppedEvent(_cellName));
                                registration.Dispose();
                            }
                        }
                        catch (Exception exception)
                        {
                            // Fatal Error
                            _observer.TryNotify(() => new CloudRuntimeFatalErrorProcessRestartEvent(exception, _cellName));
                            cancellationToken.WaitHandle.WaitOne(DelayWhenFlooding);
                            continue;
                        }
                        finally
                        {
                            AppDomain.Unload(domain);
                        }
                    }

                    completionSource.TrySetCanceled();
                });

            thread.Start();

            return completionSource.Task;
        }

        public void Reconfigure(byte[] newPackageConfig)
        {
            _packageConfig = newPackageConfig;
            var entryPoint = _entryPoint;
            if (entryPoint != null)
            {
                entryPoint.ShutdownWait();

                // will automatically re-run with the new configuration (loop in Run-method),
                // provided there's no unhandled exception and the process is not cancelled yet.
            }
        }

        public void ApplySettings(CloudServicesSettings newServicesSettings)
        {
            _servicesSettings = newServicesSettings;
            var entryPoint = _entryPoint;
            if (entryPoint != null)
            {
                // TODO: Consider a better, incremental approach

                entryPoint.ShutdownWait();

                // will automatically re-run with the new configuration (loop in Run-method),
                // provided there's no unhandled exception and the process is not cancelled yet.
            }
        }
    }
}
