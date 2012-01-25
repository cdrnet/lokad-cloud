#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Threading;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.Instrumentation;
using Lokad.Cloud.ServiceFabric;

namespace Lokad.Cloud.EntryPoint
{
    public class CloudServiceRunner
    {
        Scheduler _scheduler;

        public void Run(IApplicationEnvironment environment, Func<IRuntimeFinalizer, List<CloudService>> createServices, Action disposeServices, ILog log, IRuntimeObserver runtimeObserver, CancellationToken cancellationToken)
        {
            var finalizer = new RuntimeFinalizer();
            try
            {
                log.TryDebugFormat("Runtime: started on worker {0}.", environment.Host.WorkerName);

                var services = createServices(finalizer);

                // Execute
                _scheduler = new Scheduler(services, service => service.Start(), runtimeObserver);
                _scheduler.RunSchedule(cancellationToken);

                log.TryDebugFormat("Runtime: Runtime has stopped cleanly on worker {0}.", environment.Host.WorkerName);
            }
            catch (TypeLoadException typeLoadException)
            {
                log.TryErrorFormat(typeLoadException, "Runtime: Type {0} could not be loaded. The Runtime will be restarted.",
                    typeLoadException.TypeName);
            }
            catch (FileLoadException fileLoadException)
            {
                // Tentatively: referenced assembly is missing
                log.TryFatal("Runtime: Could not load assembly probably due to a missing reference assembly. The Runtime will be restarted.",
                    fileLoadException);
            }
            catch (SecurityException securityException)
            {
                // Tentatively: assembly cannot be loaded due to security config
                log.TryFatalFormat(securityException, "Runtime: Could not load assembly {0} probably due to security configuration. The Runtime will be restarted.",
                    securityException.FailedAssemblyInfo);
            }
            catch (ThreadInterruptedException)
            {
                log.TryWarnFormat("Runtime: execution was interrupted on worker {0} in service {1}. The Runtime will be restarted.",
                    environment.Host.WorkerName, GetNameOfServiceInExecution());
            }
            catch (ThreadAbortException)
            {
                Thread.ResetAbort();
                log.TryDebugFormat("Runtime: execution was aborted on worker {0} in service {1}. The Runtime is stopping.",
                    environment.Host.WorkerName, GetNameOfServiceInExecution());
            }
            catch (TimeoutException)
            {
                log.TryWarnFormat("Runtime: execution timed out on worker {0} in service {1}. The Runtime will be restarted.",
                    environment.Host.WorkerName, GetNameOfServiceInExecution());
            }
            catch (Exception ex)
            {
                log.TryErrorFormat(ex, "Runtime: An unhandled {0} exception occurred on worker {1} in service {2}. The Runtime will be restarted.",
                    ex.GetType().Name, environment.Host.WorkerName, GetNameOfServiceInExecution());
            }
            finally
            {
                finalizer.FinalizeRuntime();

                log.TryDebugFormat("Runtime: stopped on worker {0}.", environment.Host.WorkerName);

                if (disposeServices != null)
                {
                    disposeServices();
                }
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
    }
}
