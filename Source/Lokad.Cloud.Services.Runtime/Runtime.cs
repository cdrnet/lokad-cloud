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
        private const string ServiceSettingsBlobName = "services.settings.lokadcloud";

        private readonly CloudStorageProviders _storage;
        private readonly ICloudRuntimeObserver _observer;

        private string _packageETag, _configETag, _settingsETag;

        public Runtime(CloudStorageProviders storage, ICloudRuntimeObserver observer = null)
        {
            _storage = storage;
            _observer = observer;
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

            var completionSource = new TaskCompletionSource<object>(TaskCreationOptions.LongRunning);
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

                            // TODO: this might throw
                            cancelNewRuntime.Cancel();
                        }
                    }
                });

            thread.Start();

            return completionSource.Task;
        }

        Task RunRuntime(CancellationToken cancellationToken)
        {
            var completionSource = new TaskCompletionSource<object>(TaskCreationOptions.LongRunning);

            // Classic thread for now, can be replaced with main thread once the legacy runtime is dropped
            var thread = new Thread(() =>
                {
                    try
                    {
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
                });

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

                var serviceSettings = _storage.NeutralBlobStorage.UpsertBlobOrSkip<CloudServicesSettings>(ContainerName, ServiceSettingsBlobName,
                    insert: () => UpdateSettingsFromDefinitionIfNeeded(new CloudServicesSettings(), appDefinition.Value),
                    update: oldSettings => UpdateSettingsFromDefinitionIfNeeded(oldSettings, appDefinition.Value));

                if (!serviceSettings.HasValue)
                {
                    serviceSettings = _storage.NeutralBlobStorage.GetBlob<CloudServicesSettings>(ContainerName, ServiceSettingsBlobName);
                }

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
                    cancellationToken);
            }
        }

        void UpdatePackageConfigIfModified(RuntimeWorkingSet workingSet)
        {
            string newETag;
            var blob = _storage.NeutralBlobStorage.GetBlobIfModified<byte[]>(ContainerName, PackageConfigBlobName, _configETag, out newETag);
            if (blob.HasValue)
            {
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

            string newETag;
            var blob = _storage.NeutralBlobStorage.GetBlobIfModified<CloudServicesSettings>(ContainerName, ServiceSettingsBlobName, _settingsETag, out newETag);
            if (blob.HasValue)
            {
                var serviceSettings = _storage.NeutralBlobStorage.UpsertBlobOrSkip<CloudServicesSettings>(ContainerName, ServiceSettingsBlobName,
                    insert: () => UpdateSettingsFromDefinitionIfNeeded(new CloudServicesSettings(), applicationDefinition),
                    update: oldSettings => UpdateSettingsFromDefinitionIfNeeded(oldSettings, applicationDefinition));

                if (serviceSettings.HasValue)
                {
                    // we had to correct the new settings -> process the changes the next time
                    return;
                }

                workingSet.Rearrange(ArrangeCellsFromSettings(serviceSettings.Value));
                _settingsETag = newETag;
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

        static Maybe<CloudServicesSettings> UpdateSettingsFromDefinitionIfNeeded(CloudServicesSettings settings, CloudApplicationDefinition definition)
        {
            // TODO (ruegg, 2011-07-04): simplify, refactor

            bool changed = false;

            if (settings.QueuedCloudServices == null)
            {
                settings.QueuedCloudServices = new List<QueuedCloudServiceSettings>();
            }
            foreach (var queuedCloudService in definition.QueuedCloudServices)
            {
                if (!settings.QueuedCloudServices.Exists(s => s.TypeName == queuedCloudService.TypeName))
                {
                    changed = true;
                    settings.QueuedCloudServices.Add(new QueuedCloudServiceSettings
                    {
                        TypeName = queuedCloudService.TypeName,
                        CellAffinity = new List<string> { "default" },
                        ProcessingTimeout = new TimeSpan(1, 58, 0),
                        MessageTypeName = queuedCloudService.MessageTypeName,
                        QueueName = queuedCloudService.QueueName,
                        VisibilityTimeout = TimeSpan.FromHours(2),
                        ContinueProcessingIfMessagesAvailable = TimeSpan.FromMinutes(1),
                        MaxProcessingTrials = 5
                    });
                }
            }

            if (settings.ScheduledCloudServices == null)
            {
                settings.ScheduledCloudServices = new List<ScheduledCloudServiceSettings>();
            }
            foreach (var scheduledCloudService in definition.ScheduledCloudServices)
            {
                if (!settings.ScheduledCloudServices.Exists(s => s.TypeName == scheduledCloudService.TypeName))
                {
                    changed = true;
                    settings.ScheduledCloudServices.Add(new ScheduledCloudServiceSettings
                    {
                        TypeName = scheduledCloudService.TypeName,
                        CellAffinity = new List<string> { "default" },
                        ProcessingTimeout = new TimeSpan(1, 58, 0),
                        TriggerInterval = TimeSpan.FromHours(1)
                    });
                }
            }

            if (settings.ScheduledWorkerServices == null)
            {
                settings.ScheduledWorkerServices = new List<ScheduledWorkerServiceSettings>();
            }
            foreach (var scheduledWorkerService in definition.ScheduledWorkerServices)
            {
                if (!settings.ScheduledWorkerServices.Exists(s => s.TypeName == scheduledWorkerService.TypeName))
                {
                    changed = true;
                    settings.ScheduledWorkerServices.Add(new ScheduledWorkerServiceSettings
                    {
                        TypeName = scheduledWorkerService.TypeName,
                        CellAffinity = new List<string> { "default" },
                        ProcessingTimeout = new TimeSpan(1, 58, 0),
                        TriggerInterval = TimeSpan.FromHours(1)
                    });
                }
            }

            if (settings.DaemonServices == null)
            {
                settings.DaemonServices = new List<DaemonServiceSettings>();
            }
            foreach (var daemonService in definition.DaemonServices)
            {
                if (!settings.DaemonServices.Exists(s => s.TypeName == daemonService.TypeName))
                {
                    changed = true;
                    settings.DaemonServices.Add(new DaemonServiceSettings
                    {
                        TypeName = daemonService.TypeName,
                        CellAffinity = new List<string> { "default" }
                    });
                }
            }

            return changed ? settings : Maybe<CloudServicesSettings>.Empty;
        }
    }
}
