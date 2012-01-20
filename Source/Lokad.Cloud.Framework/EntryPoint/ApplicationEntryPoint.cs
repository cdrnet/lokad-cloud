#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading;
using System.Xml.Linq;
using Autofac;
using Autofac.Configuration;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.Management;
using Lokad.Cloud.Provisioning.Instrumentation;
using Lokad.Cloud.Provisioning.Instrumentation.Events;
using Lokad.Cloud.ServiceFabric;
using Lokad.Cloud.ServiceFabric.Runtime;
using Lokad.Cloud.Storage;
using Lokad.Cloud.Storage.Azure;

namespace Lokad.Cloud.EntryPoint
{
    public class ApplicationEntryPoint : IApplicationEntryPoint
    {
        Scheduler _scheduler;
        IRuntimeFinalizer _applicationFinalizer;

        public void Run(XElement settings, IDeploymentReader deploymentReader, IApplicationEnvironment environment, CancellationToken cancellationToken)
        {
            var log = new CloudLogWriter(CloudStorage.ForAzureConnectionString(settings.Element("DataConnectionString").Value).BuildBlobStorage());

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var currentThread = Thread.CurrentThread;
            var cancellationRegistration = cancellationToken.Register(() =>
            {
                log.DebugFormat("Runtime: Cancel() on worker {0}.", environment.Host.WorkerName);

                // TODO: Get rid of that!
                currentThread.Abort();

                if (_scheduler != null)
                {
                    _scheduler.AbortWaitingSchedule();
                }
            });

            // we need to keep the container alive until the finally block
            // because the finalizer (of the container) is called there.
            IContainer applicationContainer = null;

            try
            {
                log.DebugFormat("Runtime: started on worker {0}.", environment.Host.WorkerName);

                var applicationBuilder = new ContainerBuilder();

                applicationBuilder.RegisterModule(new StorageModule());
                applicationBuilder.RegisterModule(new DiagnosticsModule());

                applicationBuilder.RegisterType<Jobs.JobManager>();
                applicationBuilder.RegisterType<RuntimeFinalizer>().As<IRuntimeFinalizer>().InstancePerLifetimeScope();

                // NOTE: Guid is not very nice, but this will be replaced anyway once fully ported to AppHost
                applicationBuilder.Register(c =>
                    new CloudEnvironment(
                        environment,
                        c.Resolve<CloudConfigurationSettings>(),
                        c.Resolve<ILog>(),
                        c.ResolveOptional<ICloudProvisioningObserver>()))
                    .As<ICloudEnvironment, IProvisioningProvider>().SingleInstance();

                // Provisioning Observer Subject
                applicationBuilder.Register(c => new CloudProvisioningInstrumentationSubject(c.Resolve<IEnumerable<IObserver<ICloudProvisioningEvent>>>().ToArray()))
                    .As<ICloudProvisioningObserver, IObservable<ICloudProvisioningEvent>>()
                    .SingleInstance();

                applicationBuilder.RegisterInstance(new CloudConfigurationSettings
                    {
                        DataConnectionString = settings.Element("DataConnectionString").Value,
                        SelfManagementCertificateThumbprint = settings.Element("CertificateThumbprint").Value,
                        SelfManagementSubscriptionId = settings.Element("SubscriptionId").Value
                    });

                // Load Application IoC Configuration and apply it to the builder
                var autofacXml = settings.Element("AutofacAppConfig");
                if (autofacXml != null && !string.IsNullOrEmpty(autofacXml.Value))
                {
                    // HACK: need to copy settings locally first
                    // HACK: hard-code string for local storage name
                    const string fileName = "lokad.cloud.clientapp.config";
                    const string resourceName = "LokadCloudStorage";

                    var pathToFile = Path.Combine(environment.GetLocalResourcePath(resourceName), fileName);
                    File.WriteAllBytes(pathToFile, Convert.FromBase64String(autofacXml.Value));
                    applicationBuilder.RegisterModule(new ConfigurationSettingsReader("autofac", pathToFile));
                }

                // Look for all cloud services currently loaded in the AppDomain
                var serviceTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetExportedTypes()).SelectMany(x => x)
                    .Where(t => t.IsSubclassOf(typeof(CloudService)) && !t.IsAbstract && !t.IsGenericType)
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

                applicationContainer = applicationBuilder.Build();

                // Instanciate and return all the cloud services
                var services = serviceTypes.Select(type => (CloudService)applicationContainer.Resolve(type)).ToList();
                _applicationFinalizer = applicationContainer.ResolveOptional<IRuntimeFinalizer>();

                // Execute
                _scheduler = new Scheduler(services, service => service.Start(), Observers.CreateRuntimeObserver(log));
                foreach (var action in _scheduler.Schedule())
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    action();
                }

                log.DebugFormat("Runtime: Runtime has stopped cleanly on worker {0}.", environment.Host.WorkerName);
            }
            catch (TypeLoadException typeLoadException)
            {
                log.ErrorFormat(typeLoadException, "Runtime: Type {0} could not be loaded. The Runtime will be restarted.",
                    typeLoadException.TypeName);
            }
            catch (FileLoadException fileLoadException)
            {
                // Tentatively: referenced assembly is missing
                log.Fatal(fileLoadException, "Runtime: Could not load assembly probably due to a missing reference assembly. The Runtime will be restarted.");
            }
            catch (SecurityException securityException)
            {
                // Tentatively: assembly cannot be loaded due to security config
                log.FatalFormat(securityException, "Runtime: Could not load assembly {0} probably due to security configuration. The Runtime will be restarted.",
                    securityException.FailedAssemblyInfo);
            }
            catch (TriggerRestartException)
            {
                log.DebugFormat("Runtime: Triggered to stop execution on worker {0} in service {1}. The Role Instance will be recycled and the Runtime restarted.",
                    environment.Host.WorkerName, GetNameOfServiceInExecution());

                environment.LoadCurrentHeadDeployment();
                throw;
            }
            catch (ThreadInterruptedException)
            {
                log.WarnFormat("Runtime: execution was interrupted on worker {0} in service {1}. The Runtime will be restarted.",
                    environment.Host.WorkerName, GetNameOfServiceInExecution());
            }
            catch (ThreadAbortException)
            {
                Thread.ResetAbort();
                log.DebugFormat("Runtime: execution was aborted on worker {0} in service {1}. The Runtime is stopping.",
                    environment.Host.WorkerName, GetNameOfServiceInExecution());
            }
            catch (TimeoutException)
            {
                log.WarnFormat("Runtime: execution timed out on worker {0} in service {1}. The Runtime will be restarted.",
                    environment.Host.WorkerName, GetNameOfServiceInExecution());
            }
            catch (Exception ex)
            {
                log.ErrorFormat(ex, "Runtime: An unhandled {0} exception occurred on worker {1} in service {2}. The Runtime will be restarted.",
                    ex.GetType().Name, environment.Host.WorkerName, GetNameOfServiceInExecution());
            }
            finally
            {
                cancellationRegistration.Dispose();

                log.DebugFormat("Runtime: stopping on worker {0}.", environment.Host.WorkerName);

                if (_applicationFinalizer != null)
                {
                    _applicationFinalizer.FinalizeRuntime();
                }

                if (applicationContainer != null)
                {
                    applicationContainer.Dispose();
                }

                log.DebugFormat("Runtime: stopped on worker {0}.", environment.Host.WorkerName);
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

        public void OnSettingsChanged(XElement settings)
        {
        }
    }
}
