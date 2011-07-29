#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.AppHost.Framework.Events;
using Lokad.Cloud.AppHost.Util;

namespace Lokad.Cloud.AppHost
{
    internal sealed class Cell
    {
        private static readonly TimeSpan FloodFrequencyThreshold = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan DelayWhenFlooding = TimeSpan.FromMinutes(5);

        private readonly CancellationTokenSource _cancellationTokenSource;

        private readonly IHostContext _hostContext;
        private readonly HostHandle _hostHandle;
        private readonly CellHandle _cellHandle;

        private volatile CellProcessAppDomainEntryPoint _entryPoint;
        private volatile XElement _cellDefinition;
        private volatile string _deploymentName;

        private Cell(IHostContext hostContext, HostHandle hostHandle, XElement cellDefinition, string deploymentName, CancellationToken cancellationToken)
        {
            _hostContext = hostContext;
            _hostHandle = hostHandle;
            _cellHandle = new CellHandle(cellDefinition.AttributeValue("name"));
            _cellDefinition = cellDefinition;
            _deploymentName = deploymentName;
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        }

        public static Cell Run(
            IHostContext hostContext,
            HostHandle hostHandle,
            XElement cellDefinition,
            string deploymentName,
            CancellationToken cancellationToken)
        {
            var process = new Cell(hostContext, hostHandle, cellDefinition, deploymentName, cancellationToken);
            process.Run();
            return process;
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
                            domain.UnhandledException += (sender, args) => _hostHandle.TryNotify(() => new CloudRuntimeExceptionProcessRestartingEvent(args.ExceptionObject as Exception, _cellHandle.CellName, false));

                            try
                            {
                                _entryPoint = (CellProcessAppDomainEntryPoint)domain.CreateInstanceAndUnwrap(
                                    Assembly.GetExecutingAssembly().FullName,
                                    typeof(CellProcessAppDomainEntryPoint).FullName);
                            }
                            catch (Exception exception)
                            {
                                // Fatal Error
                                _hostHandle.TryNotify(() => new CloudRuntimeFatalErrorProcessRestartEvent(exception, _cellHandle.CellName));
                                cancellationToken.WaitHandle.WaitOne(DelayWhenFlooding);
                                continue;
                            }

                            // Forward cancellation token to AppDomain-internal cancellation token source
                            var registration = cancellationToken.Register(_entryPoint.Cancel);
                            try
                            {
                                _hostHandle.TryNotify(() => new CloudRuntimeCellStartedEvent(_cellHandle.CellName));

                                _cellHandle.CurrentDeploymentName = _deploymentName;
                                _cellHandle.CurretAssembliesName = _cellDefinition.SettingsElementAttributeValue("Assemblies", "name");

                                _entryPoint.Run(_cellDefinition.ToString(), _hostContext.DeploymentReader, new ApplicationEnvironment(_hostContext, _hostHandle, _cellHandle));
                            }
                            catch (Exception exception)
                            {
                                _entryPoint = null;
                                if ((DateTimeOffset.UtcNow - lastRoundStartTime) < FloodFrequencyThreshold)
                                {
                                    _hostHandle.TryNotify(() => new CloudRuntimeExceptionProcessRestartingEvent(exception, _cellHandle.CellName, true));
                                    cancellationToken.WaitHandle.WaitOne(DelayWhenFlooding);
                                }
                                else
                                {
                                    _hostHandle.TryNotify(() => new CloudRuntimeExceptionProcessRestartingEvent(exception, _cellHandle.CellName, false));
                                }
                                continue;
                            }
                            finally
                            {
                                _entryPoint = null;
                                _hostHandle.TryNotify(() => new CloudRuntimeCellStoppedEvent(_cellHandle.CellName));
                                registration.Dispose();
                            }
                        }
                        catch (Exception exception)
                        {
                            // Fatal Error
                            _hostHandle.TryNotify(() => new CloudRuntimeFatalErrorProcessRestartEvent(exception, _cellHandle.CellName));
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

        public void ApplyChangedCellDefinition(XElement newCellDefinition, string newDeploymentName)
        {
            var oldCellDefinition = _cellDefinition;
            var newAssembliesName = newCellDefinition.SettingsElementAttributeValue("Assemblies", "name");
            var newRunnerTypeName = newCellDefinition.SettingsElementAttributeValue("Runner", "typeName");

            _cellDefinition = newCellDefinition;
            _deploymentName = newDeploymentName;

            var entryPoint = _entryPoint;
            if (entryPoint == null)
            {
                return;
            }

            if (oldCellDefinition.SettingsElementAttributeValue("Assemblies", "name") != newAssembliesName
                || oldCellDefinition.SettingsElementAttributeValue("Runner", "typeName") != newRunnerTypeName)
            {
                // cancel will stop the cell and unload the AppDomain, but then automatically
                // start again with the new assemblies and runner
                entryPoint.Cancel();
                return;
            }

            entryPoint.AppplyChangedSettings((newCellDefinition.Element("Settings") ?? new XElement("Settings")).ToString());
        }
    }
}
