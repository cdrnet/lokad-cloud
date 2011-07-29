#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Lokad.Cloud.Services.Framework.Instrumentation.Events;
using Lokad.Cloud.Services.Runtime.Internal;

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

        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly byte[] _packageAssemblies;
        private readonly RuntimeHandle _runtimeHandle;
        private readonly CellHandle _cellHandle;

        private volatile CellProcessAppDomainEntryPoint _entryPoint;
        private volatile XElement _servicesSettings;
        private volatile byte[] _packageConfig;

        private CellProcess(CancellationToken cancellationToken, RuntimeHandle runtimeHandle, CellHandle cellHandle, byte[] packageAssemblies, byte[] packageConfig, XElement servicesSettings)
        {
            _runtimeHandle = runtimeHandle;
            _cellHandle = cellHandle;
            _packageAssemblies = packageAssemblies;
            _packageConfig = packageConfig;
            _servicesSettings = servicesSettings;
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        }

        public static CellProcess Run(
            byte[] packageAssemblies,
            byte[] packageConfig,
            XElement servicesSettings,
            RuntimeHandle runtimeHandle,
            CellHandle cellHandle,
            CancellationToken cancellationToken)
        {
            var process = new CellProcess(cancellationToken, runtimeHandle, cellHandle, packageAssemblies, packageConfig, servicesSettings);
            process.Run();
            return process;
        }

        public string CellName
        {
            // TODO: drop
            get { return _cellHandle.CellName; }
        }

        public Task Task { get; private set; }

        /// <summary>
        /// Shutdown just this cell. Use the Task property to wait for the shutdown to complete if needed.
        /// </summary>
        public void Cancel()
        {
            _cancellationTokenSource.Cancel();
        }

        void Run()
        {
            var cancellationToken = _cancellationTokenSource.Token;
            var completionSource = new TaskCompletionSource<object>();
            Task = completionSource.Task;

            var thread = new Thread(() =>
                {
                    var currentRoundStartTime = DateTimeOffset.UtcNow - FloodFrequencyThreshold;
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var lastRoundStartTime = currentRoundStartTime;
                        currentRoundStartTime = DateTimeOffset.UtcNow;

                        AppDomain domain = AppDomain.CreateDomain("LokadCloudServiceRuntimeCell_" + _cellHandle.CellName, null, AppDomain.CurrentDomain.SetupInformation);
                        try
                        {
                            domain.UnhandledException += (sender, args) => _runtimeHandle.TryNotify(() => new CloudRuntimeExceptionProcessRestartingEvent(args.ExceptionObject as Exception, _cellHandle.CellName, false));

                            try
                            {
                                _entryPoint = (CellProcessAppDomainEntryPoint)domain.CreateInstanceAndUnwrap(
                                    Assembly.GetExecutingAssembly().FullName, typeof(CellProcessAppDomainEntryPoint).FullName);
                            }
                            catch (Exception exception)
                            {
                                // Fatal Error
                                _runtimeHandle.TryNotify(() => new CloudRuntimeFatalErrorProcessRestartEvent(exception, _cellHandle.CellName));
                                cancellationToken.WaitHandle.WaitOne(DelayWhenFlooding);
                                continue;
                            }

                            // Forward cancellation token to AppDomain-internal cancellation token source
                            var registration = cancellationToken.Register(_entryPoint.Cancel);
                            try
                            {
                                _runtimeHandle.TryNotify(() => new CloudRuntimeCellStartedEvent(_cellHandle.CellName));
                                _entryPoint.Run(_packageAssemblies, _packageConfig, _servicesSettings.ToString(), new RuntimeCellEnvironment(_runtimeHandle, _cellHandle));
                            }
                            catch (Exception exception)
                            {
                                _entryPoint = null;
                                if ((DateTimeOffset.UtcNow - lastRoundStartTime) < FloodFrequencyThreshold)
                                {
                                    _runtimeHandle.TryNotify(() => new CloudRuntimeExceptionProcessRestartingEvent(exception, _cellHandle.CellName, true));
                                    cancellationToken.WaitHandle.WaitOne(DelayWhenFlooding);
                                }
                                else
                                {
                                    _runtimeHandle.TryNotify(() => new CloudRuntimeExceptionProcessRestartingEvent(exception, _cellHandle.CellName, false));
                                }
                                continue;
                            }
                            finally
                            {
                                _entryPoint = null;
                                _runtimeHandle.TryNotify(() => new CloudRuntimeCellStoppedEvent(_cellHandle.CellName));
                                registration.Dispose();
                            }
                        }
                        catch (Exception exception)
                        {
                            // Fatal Error
                            _runtimeHandle.TryNotify(() => new CloudRuntimeFatalErrorProcessRestartEvent(exception, _cellHandle.CellName));
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

            thread.Name = "Lokad.Cloud Cell Process (" + _cellHandle.CellName + ")";
            thread.Start();
        }

        public void Reconfigure(byte[] newPackageConfig)
        {
            _packageConfig = newPackageConfig;
            var entryPoint = _entryPoint;
            if (entryPoint != null)
            {
                entryPoint.Reconfigure(newPackageConfig);
            }
        }

        public void ApplySettings(XElement newServicesSettings)
        {
            _servicesSettings = newServicesSettings;
            var entryPoint = _entryPoint;
            if (entryPoint != null)
            {
                entryPoint.ApplySettings(newServicesSettings.ToString());
            }
        }
    }
}
