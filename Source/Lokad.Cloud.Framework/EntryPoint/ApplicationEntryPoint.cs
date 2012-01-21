#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;
using System.Security;
using System.Threading;
using System.Xml.Linq;
using Autofac;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.ServiceFabric;

namespace Lokad.Cloud.EntryPoint
{
    // NOTE: referred to by name in WorkerRole DeploymentReader
    public class ApplicationEntryPoint : IApplicationEntryPoint
    {
        Scheduler _scheduler;

        public void Run(XElement settings, IDeploymentReader deploymentReader, IApplicationEnvironment environment, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var factory = (ICloudFactory)Activator.CreateInstance(Type.GetType(settings.Element("CloudFactoryTypeName").Value));
            factory.Initialize(environment, settings);

            var log = factory.Log;

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
            var applicationFinalizer = new RuntimeFinalizer();

            try
            {
                log.DebugFormat("Runtime: started on worker {0}.", environment.Host.WorkerName);

                var services = factory.CreateServices(applicationFinalizer, out applicationContainer);

                // Execute
                _scheduler = new Scheduler(services, service => service.Start(), factory.CreateRuntimeObserverOptional());
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

                if (applicationFinalizer != null)
                {
                    applicationFinalizer.FinalizeRuntime();
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
