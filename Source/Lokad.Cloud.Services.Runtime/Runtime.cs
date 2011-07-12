#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Cloud.Services.Framework.Instrumentation;
using Lokad.Cloud.Services.Framework.Instrumentation.Events;
using Lokad.Cloud.Services.Management.Application;
using Lokad.Cloud.Services.Management.Settings;
using Lokad.Cloud.Services.Runtime.Legacy;
using Lokad.Cloud.Services.Runtime.WorkingSet;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.Services.Runtime
{
    /// <summary>
    /// Fully managed storage-based entry point for cloud service hosting.
    /// </summary>
    /// <remarks>
    /// Primary responsibility of this class: To build and manage a runtime working set
    /// based on the app package and settings in the storage, to update or rebuild settings
    /// to match the app, and to update the working set if anything changes from the outside
    /// (new package, changed config, changed service settings).
    /// </remarks>
    public sealed class Runtime
    {
        // The current implementation hosts this new runtime but also the old
        // legacy runtime (in a separte thread); the latter will be dropped in the future.

        private const string ContainerName = "lokad-cloud-services";
        private const string PackageAssembliesBlobName = "package.assemblies.lokadcloud";
        private const string PackageConfigBlobName = "package.config.lokadcloud";

        private readonly CloudStorageProviders _storage;
        private readonly ICloudRuntimeObserver _observer;
        private readonly ServicesSettingsManager _settingsManager;

        private string _packageETag, _configETag, _settingsETag;

        public Runtime(CloudStorageProviders storage, ICloudRuntimeObserver observer = null)
        {
            _storage = storage;
            _observer = observer;
            _settingsManager = new ServicesSettingsManager(storage);
        }

        public Task Run(CancellationToken cancellationToken)
        {
            var cancelNewTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            return Task.Factory.ContinueWhenAll(
                new[] { RunRuntime(cancelNewTokenSource.Token), RunLegacyRuntime(cancellationToken, cancelNewTokenSource) },
                tasks => { });
        }

        [Obsolete]
        Task RunLegacyRuntime(CancellationToken cancellationToken, CancellationTokenSource cancelNewRuntime)
        {
            // TODO: refactor -> simplify -> drop

            var host = new ServiceFabricHost();
            host.StartRuntime();

            var completionSource = new TaskCompletionSource<object>();
            cancellationToken.Register(host.ShutdownRuntime);

            // Classic thread because the legacy runtime was designed to run in the main thread exclusively
            var thread = new Thread(() =>
                {
                    try
                    {
                        host.Run();
                        completionSource.TrySetResult(null);
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
                    finally
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            // cancel the other runtime, so we can recycle
                            // (expected behavior of the legacy runtime)

                            // TODO: this may throw
                            cancelNewRuntime.Cancel();
                        }
                    }
                });

            thread.Name = "Lokad.Cloud Legacy Runtime";
            thread.Start();

            return completionSource.Task;
        }

        Task RunRuntime(CancellationToken cancellationToken)
        {
            var completionSource = new TaskCompletionSource<object>();

            // Classic thread for now, can be replaced with main thread once the legacy runtime is dropped
            var thread = new Thread(() =>
                {
                    try
                    {
                        _observer.TryNotify(() => new CloudRuntimeStartedEvent());

                        CloudApplicationDefinition applicationDefinition = null;
                        var workingSet = RuntimeWorkingSet.Empty;

                        while (!cancellationToken.IsCancellationRequested)
                        {
                            UpdatePackageAssembliesIfModified(cancellationToken, ref workingSet, ref applicationDefinition);
                            UpdatePackageConfigIfModified(workingSet);
                            UpdateServiceSettingsIfModified(workingSet, applicationDefinition);

                            cancellationToken.WaitHandle.WaitOne(TimeSpan.FromMinutes(1));
                        }
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
                    finally
                    {
                        _observer.TryNotify(() => new CloudRuntimeStoppedEvent());
                    }
                });

            thread.Name = "Lokad.Cloud Runtime";
            thread.Start();

            return completionSource.Task;
        }

        void UpdatePackageAssembliesIfModified(CancellationToken cancellationToken, ref RuntimeWorkingSet workingSet, ref CloudApplicationDefinition applicationDefinition)
        {
            string newPackageETag;
            var packageAssemblies = _storage.NeutralBlobStorage.GetBlobIfModified<byte[]>(ContainerName, PackageAssembliesBlobName, _packageETag, out newPackageETag);
            if (!packageAssemblies.HasValue && newPackageETag == null)
            {
                // NO PACKAGE -> RETRY LATER
                cancellationToken.WaitHandle.WaitOne(TimeSpan.FromMinutes(4));
                return;
            }
            if (packageAssemblies.HasValue)
            {
                // NEW PACKAGE

                if (_packageETag != null)
                {
                    _observer.TryNotify(() => new CloudRuntimeAppPackageChangedEvent());
                }

                // => STOP

                // TODO: this may throw
                workingSet.ShutdownWait();

                // => REBUILD

                var inspector = new CloudApplicationInspector(_storage);
                var appDefinition = inspector.Inspect();

                if (!appDefinition.HasValue)
                {
                    // INSPECTION FAILED (RACE) -> RETRY LATER
                    workingSet = RuntimeWorkingSet.Empty;
                    cancellationToken.WaitHandle.WaitOne(TimeSpan.FromMinutes(1));
                    return;
                }

                applicationDefinition = appDefinition.Value;

                var serviceSettings = _settingsManager.UpdateAndLoadSettings(appDefinition.Value, out _settingsETag);
                if (!serviceSettings.HasValue)
                {
                    // SETTINGS UNAVAILABLE (RACE) -> RETRY LATER
                    workingSet = RuntimeWorkingSet.Empty;
                    cancellationToken.WaitHandle.WaitOne(TimeSpan.FromMinutes(1));
                    return;
                }

                // => START

                var packageConfig = _storage.NeutralBlobStorage.GetBlob<byte[]>(ContainerName, PackageConfigBlobName, out _configETag);
                _packageETag = newPackageETag;

                workingSet = RuntimeWorkingSet.StartNew(
                    packageAssemblies.Value, packageConfig.GetValue(new byte[0]),
                    ArrangeCellsFromSettings(serviceSettings.Value),
                    _observer,
                    cancellationToken);

                _observer.TryNotify(() => new CloudRuntimeAppPackageLoadedEvent(appDefinition.Value.Timestamp, appDefinition.Value.PackageETag));
            }
        }

        void UpdatePackageConfigIfModified(RuntimeWorkingSet workingSet)
        {
            string newETag;
            var blob = _storage.NeutralBlobStorage.GetBlobIfModified<byte[]>(ContainerName, PackageConfigBlobName, _configETag, out newETag);
            if (blob.HasValue)
            {
                _observer.TryNotify(() => new CloudRuntimeAppConfigChangedEvent());
                workingSet.Reconfigure(blob.Value);
                _configETag = newETag;
            }
        }

        void UpdateServiceSettingsIfModified(RuntimeWorkingSet workingSet, CloudApplicationDefinition applicationDefinition)
        {
            if (applicationDefinition == null)
            {
                return;
            }

            if (_settingsManager.HaveSettingsChanged(_settingsETag))
            {
                // make sure the settings still fit to the application
                string newEtag;
                var serviceSettings = _settingsManager.UpdateAndLoadSettings(applicationDefinition, out newEtag);
                if (!serviceSettings.HasValue)
                {
                    // SETTINGS UNAVAILABLE (RACE) -> RETRY LATER
                    return;
                }

                _observer.TryNotify(() => new CloudRuntimeServiceSettingsChangedEvent());
                workingSet.Rearrange(ArrangeCellsFromSettings(serviceSettings.Value));
                _settingsETag = newEtag;
            }
        }

        static IEnumerable<CellArrangement> ArrangeCellsFromSettings(CloudServicesSettings serviceSettings)
        {
            var cellAffinities = new List<string>();
            cellAffinities.AddRange(serviceSettings.QueuedCloudServices.SelectMany(s => s.CellAffinity));
            cellAffinities.AddRange(serviceSettings.ScheduledCloudServices.SelectMany(s => s.CellAffinity));
            cellAffinities.AddRange(serviceSettings.ScheduledWorkerServices.SelectMany(s => s.CellAffinity));
            cellAffinities.AddRange(serviceSettings.DaemonServices.SelectMany(s => s.CellAffinity));

            return cellAffinities.Distinct().ToList().Select(name =>
                new CellArrangement(name,
                    serviceSettings.QueuedCloudServices.Where(s => !s.IsDisabled && s.CellAffinity.Contains(name)).ToArray(),
                    serviceSettings.ScheduledCloudServices.Where(s => !s.IsDisabled && s.CellAffinity.Contains(name)).ToArray(),
                    serviceSettings.ScheduledWorkerServices.Where(s => !s.IsDisabled && s.CellAffinity.Contains(name)).ToArray(),
                    serviceSettings.DaemonServices.Where(s => !s.IsDisabled && s.CellAffinity.Contains(name)).ToArray()));
        }
    }
}
