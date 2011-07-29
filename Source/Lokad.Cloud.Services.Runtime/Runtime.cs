#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Cloud.Services.Framework.Commands;
using Lokad.Cloud.Services.Framework.Instrumentation;
using Lokad.Cloud.Services.Framework.Instrumentation.Events;
using Lokad.Cloud.Services.Management.Deployments;
using Lokad.Cloud.Services.Runtime.Agents;
using Lokad.Cloud.Services.Runtime.Internal;
using Lokad.Cloud.Services.Runtime.WorkingSet;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.Services.Runtime
{
    /// <summary>
    /// Fully managed storage-based entry point for cloud service hosting.
    /// </summary>
    public sealed class Runtime
    {
        private readonly CloudStorageProviders _storage;
        private readonly ICloudRuntimeObserver _observer;
        private readonly DeploymentHeadPollingAgent _deploymentPollingAgent;
        private readonly DeploymentReader _deploymentReader;
        private readonly ConcurrentQueue<ICloudCommand> _commandQueue;

        // only accessed inside of the new runtime thread
        private DeploymentReference _currentDeployment;

        public Runtime(CloudStorageProviders storage, ICloudRuntimeObserver observer = null)
        {
            _storage = storage;
            _observer = observer;
            _deploymentReader = new DeploymentReader(storage);
            _commandQueue = new ConcurrentQueue<ICloudCommand>();
            _deploymentPollingAgent = new DeploymentHeadPollingAgent(storage, AcceptCommand);
        }

        public void Run(CancellationToken cancellationToken)
        {
            _observer.TryNotify(() => new CloudRuntimeStartedEvent());

            var workingSet = RuntimeWorkingSet.Empty;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    // 1. run agents
                    _deploymentPollingAgent.PollForChanges(_currentDeployment.Name);

                    // 2. apply commands
                    ICloudCommand command;
                    if (_commandQueue.TryDequeue(out command))
                    {
                        // dynamic dispatch, good enough for now
                        ApplyCommand((dynamic)command, ref workingSet, cancellationToken);
                    }

                    // 3. repeat, but throttled
                    cancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(30));
                }
            }
            finally
            {
                _observer.TryNotify(() => new CloudRuntimeStoppedEvent());
            }
        }

        public Task RunAsync(CancellationToken cancellationToken)
        {
            var completionSource = new TaskCompletionSource<object>();

            // Classic thread for now
            var thread = new Thread(() =>
                {
                    try
                    {
                        Run(cancellationToken);
                    }
                    catch (ThreadAbortException)
                    {
                        Thread.ResetAbort();
                        completionSource.TrySetCanceled();
                    }
                    catch (Exception exception)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            // assuming the exception was caused by the cancellation
                            completionSource.TrySetCanceled();
                        }
                        else
                        {
                            completionSource.TrySetException(exception);
                        }
                    }
                });

            thread.Name = "Lokad.Cloud Runtime";
            thread.Start();

            return completionSource.Task;
        }

        // NOTE: simplified command handling, can easily be refactored later if necessary

        public void AcceptCommand(ICloudCommand command)
        {
            _commandQueue.Enqueue(command);
        }

        void ApplyCommand(RuntimeLoadCurrentHeadDeploymentCommand command, ref RuntimeWorkingSet workingSet, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            _deploymentPollingAgent.PollForChanges(_currentDeployment.Name);
        }

        void ApplyCommand(RuntimeLoadDeploymentCommand command, ref RuntimeWorkingSet workingSet, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var current = _currentDeployment;
            if (current != null && command.DeploymentName == current.Name)
            {
                // already on requested deployment
                return;
            }

            var requestedBlob = _deploymentReader.GetDeployment(command.DeploymentName);
            if (!requestedBlob.HasValue)
            {
                // TODO: NOTIFY/LOG invalid deployment
                return;
            }

            var requested = requestedBlob.Value;

            // CASE A: Assemblies have changed -> FULL RESTART
            if (current == null || requested.AssembliesName != current.AssembliesName)
            {
                var assembliesBytes = _deploymentReader.GetBytes(requested.AssembliesName);
                var configBytes = _deploymentReader.GetBytes(requested.ConfigName);
                var settingsXml = _deploymentReader.GetXml(requested.SettingsName);
                if (!assembliesBytes.HasValue || !configBytes.HasValue || !settingsXml.HasValue)
                {
                    // TODO: NOTIFY/LOG invalid deployment
                    return;
                }

                // TODO: this may throw
                workingSet.ShutdownWait();

                _currentDeployment = requested;
                workingSet = RuntimeWorkingSet.StartNew(
                    assembliesBytes.Value,
                    configBytes.Value,
                    SettingsTools.DenormalizeSettingsByCell(settingsXml.Value),
                    _observer,
                    AcceptCommand,
                    cancellationToken);

                //_observer.TryNotify(() => new CloudRuntimeAppPackageLoadedEvent(appDefinition.Value.Timestamp, appDefinition.Value.PackageETag));

                return;
            }

            // CASE B 1) IoC Config has changed -> Reconfigure
            if (requested.ConfigName != current.ConfigName)
            {
                var configBytes = _deploymentReader.GetBytes(requested.ConfigName);
                if (!configBytes.HasValue)
                {
                    // TODO: NOTIFY/LOG invalid deployment
                    return;
                }

                workingSet.Reconfigure(configBytes.Value);
                _observer.TryNotify(() => new CloudRuntimeAppConfigChangedEvent());
            }

            // CASE B 2) Service Settings have changed -> Rearrange
            if (requested.SettingsName != current.SettingsName)
            {
                var settingsXml = _deploymentReader.GetXml(requested.SettingsName);
                if (!settingsXml.HasValue)
                {
                    // TODO: NOTIFY/LOG invalid deployment
                    return;
                }

                workingSet.Rearrange(SettingsTools.DenormalizeSettingsByCell(settingsXml.Value));
                _observer.TryNotify(() => new CloudRuntimeServiceSettingsChangedEvent());
            }
        }
    }
}
