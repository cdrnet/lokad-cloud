#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Threading;
using Lokad.Cloud.Instrumentation;
using Lokad.Cloud.Instrumentation.Events;
using Lokad.Cloud.ServiceFabric;

namespace Lokad.Cloud.EntryPoint
{
    public class CloudServiceRunner
    {
        private readonly IRuntimeObserver _runtimeObserver;

        public CloudServiceRunner(IRuntimeObserver runtimeObserver)
        {
            _runtimeObserver = runtimeObserver;
        }

        public void Run(IEnvironment environment, List<CloudService> services, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            Notify(() => new RuntimeStartedEvent(environment.Host));
            var scheduler = new Scheduler(services, service => service.Start(), _runtimeObserver);
            try
            {
                scheduler.RunSchedule(cancellationToken);
            }
            catch (ThreadInterruptedException)
            {
                Notify(() => new RuntimeInterruptedRestartedEvent(environment.Host, GetNameOfServiceInExecution(scheduler)));
            }
            catch (ThreadAbortException)
            {
                Thread.ResetAbort();
                Notify(() => new RuntimeInterruptedRestartedEvent(environment.Host, GetNameOfServiceInExecution(scheduler)));
            }
            catch (TimeoutException)
            {
                Notify(() => new RuntimeTimeoutRestartedEvent(environment.Host, GetNameOfServiceInExecution(scheduler)));
            }
            catch (Exception ex)
            {
                Notify(() => new RuntimeExceptionRestartedEvent(environment.Host, GetNameOfServiceInExecution(scheduler), ex));
            }
            finally
            {
                Notify(() => new RuntimeStoppedEvent(environment.Host));
            }
        }

        /// <summary>The name of the service that is being executed, if any, <c>null</c> otherwise.</summary>
        private string GetNameOfServiceInExecution(Scheduler scheduler)
        {
            CloudService service;
            if (scheduler == null || (service = scheduler.CurrentlyScheduledService) == null)
            {
                return "unknown";
            }

            return service.Name;
        }

        private void Notify(Func<IRuntimeEvent> buildEvent)
        {
            if (_runtimeObserver != null)
            {
                _runtimeObserver.Notify(buildEvent());
            }
        }
    }
}
