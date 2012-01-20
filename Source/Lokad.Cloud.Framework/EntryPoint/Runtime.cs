#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Autofac;
using Autofac.Configuration;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.Instrumentation;
using Lokad.Cloud.ServiceFabric;
using Lokad.Cloud.ServiceFabric.Runtime;

namespace Lokad.Cloud.EntryPoint
{
    /// <summary>Organize the executions of the services.</summary>
    internal class Runtime
    {
        readonly IRuntimeFinalizer _runtimeFinalizer;
        readonly ILog _log;
        readonly ICloudRuntimeObserver _observer;

        readonly IApplicationEnvironment _environment;
        readonly CloudConfigurationSettings _settings;
        readonly byte[] _autofacAppConfig;

        Scheduler _scheduler;
        IRuntimeFinalizer _applicationFinalizer;

        /// <summary>IoC constructor.</summary>
        public Runtime(IRuntimeFinalizer runtimeFinalizer, IApplicationEnvironment environment, CloudConfigurationSettings settings, byte[] autofacAppConfig, ILog log, ICloudRuntimeObserver observer = null)
        {
            _runtimeFinalizer = runtimeFinalizer;
            _environment = environment;
            _log = log;
            _observer = observer;

            _settings = settings;
            _autofacAppConfig = autofacAppConfig;
        }

        /// <summary>Called once by the service fabric. Call is not supposed to return
        /// until stop is requested, or an uncaught exception is thrown.</summary>
        public void Execute(CancellationToken cancellationToken)
        {
            _log.DebugFormat("Runtime: started on worker {0}.", _environment.Host.WorkerName);

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var currentThread = Thread.CurrentThread;
            var cancellationRegistration = cancellationToken.Register(() =>
                {
                    _log.DebugFormat("Runtime: Cancel() on worker {0}.", _environment.Host.WorkerName);

                    // TODO: Get rid of that!
                    currentThread.Abort();

                    if (_scheduler != null)
                    {
                        _scheduler.AbortWaitingSchedule();
                    }
                });

            IContainer applicationContainer = null;
            try
            {
                List<CloudService> services;

                // note: we need to keep the container alive until the finally block
                // because the finalizer (of the container) is called there.
                applicationContainer = LoadAndBuildApplication(out services);

                _applicationFinalizer = applicationContainer.ResolveOptional<IRuntimeFinalizer>();
                _scheduler = new Scheduler(services, service => service.Start(), _observer);

                foreach (var action in _scheduler.Schedule())
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    action();
                }
            }
            catch (ThreadInterruptedException)
            {
                _log.WarnFormat("Runtime: execution was interrupted on worker {0} in service {1}. The Runtime will be restarted.",
                    _environment.Host.WorkerName, GetNameOfServiceInExecution());
            }
            catch (ThreadAbortException)
            {
                Thread.ResetAbort();

                _log.DebugFormat("Runtime: execution was aborted on worker {0} in service {1}. The Runtime is stopping.",
                    _environment.Host.WorkerName, GetNameOfServiceInExecution());
            }
            catch (TimeoutException)
            {
                _log.WarnFormat("Runtime: execution timed out on worker {0} in service {1}. The Runtime will be restarted.",
                    _environment.Host.WorkerName, GetNameOfServiceInExecution());
            }
            catch (TriggerRestartException)
            {
                // Supposed to be handled by the runtime host (i.e. AppDomainEntryPoint)
                throw;
            }
            catch (Exception ex)
            {
                _log.ErrorFormat(ex, "Runtime: An unhandled {0} exception occurred on worker {1} in service {2}. The Runtime will be restarted.",
                    ex.GetType().Name, _environment.Host.WorkerName, GetNameOfServiceInExecution());
            }
            finally
            {
                cancellationRegistration.Dispose();

                _log.DebugFormat("Runtime: stopping on worker {0}.", _environment.Host.WorkerName);

                if (_runtimeFinalizer != null)
                {
                    _runtimeFinalizer.FinalizeRuntime();
                }

                if (_applicationFinalizer != null)
                {
                    _applicationFinalizer.FinalizeRuntime();
                }

                if (applicationContainer != null)
                {
                    applicationContainer.Dispose();
                }

                _log.DebugFormat("Runtime: stopped on worker {0}.", _environment.Host.WorkerName);
            }
        }

        /// <summary>The name of the service that is being executed, if any, <c>null</c> otherwise.</summary>
        private string GetNameOfServiceInExecution()
        {
            var scheduler = _scheduler;
            CloudService service;
            if (scheduler == null || (service = scheduler.CurrentlyScheduledService) == null)
            {
                return "unknown";
            }

            return service.Name;
        }

        /// <summary>
        /// Load and get all initialized service instances using the provided IoC container.
        /// </summary>
        IContainer LoadAndBuildApplication(out List<CloudService> services)
        {
            var applicationBuilder = new ContainerBuilder();
            applicationBuilder.RegisterModule(new CloudModule());
            applicationBuilder.RegisterInstance(_settings);

            // Load Application IoC Configuration and apply it to the builder
            if (_autofacAppConfig != null && _autofacAppConfig.Length > 0)
            {
                // HACK: need to copy settings locally first
                // HACK: hard-code string for local storage name
                const string fileName = "lokad.cloud.clientapp.config";
                const string resourceName = "LokadCloudStorage";

                var pathToFile = Path.Combine(_environment.GetLocalResourcePath(resourceName), fileName);
                File.WriteAllBytes(pathToFile, _autofacAppConfig);
                applicationBuilder.RegisterModule(new ConfigurationSettingsReader("autofac", pathToFile));
            }

            // Look for all cloud services currently loaded in the AppDomain
            var serviceTypes = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetExportedTypes()).SelectMany(x => x)
                .Where(t => t.IsSubclassOf(typeof (CloudService)) && !t.IsAbstract && !t.IsGenericType)
                .ToList();

            // Register the cloud services in the IoC Builder so we can support dependencies
            foreach (var type in serviceTypes)
            {
                applicationBuilder.RegisterType(type)
                    .OnActivating(e =>
                        {
                            e.Context.InjectUnsetProperties(e.Instance);

                            var initializable = e.Instance as IInitializable;
                            if (initializable != null)
                            {
                                initializable.Initialize();
                            }
                        })
                    .InstancePerDependency()
                    .ExternallyOwned();

                // ExternallyOwned: to prevent the container from disposing the
                // cloud services - we manage their lifetime on our own using
                // e.g. RuntimeFinalizer
            }

            var applicationContainer = applicationBuilder.Build();

            // Instanciate and return all the cloud services
            services = serviceTypes.Select(type => (CloudService)applicationContainer.Resolve(type)).ToList();

            return applicationContainer;
        }
    }
}