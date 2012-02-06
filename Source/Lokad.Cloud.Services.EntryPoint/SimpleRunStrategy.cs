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

namespace Lokad.Cloud.Services.EntryPoint
{
    /// <summary>
    /// Runs the services infinitely with cooperative round robin scheduling
    /// until cancelled. Restarts (without recycling) if settings change.
    /// </summary>
    public class SimpleRunStrategy
    {
        private CancellationTokenSource _cancelledOrSettingsChangedCts;

        public void RunInfinitely(
            IEnvironment environment,
            CancellationToken cancellationToken,
            Action<Action<IRuntimeObserver, List<CloudService>>> runOnce)
        {
            // We want to recycle if the settings change, but not actually unload the AppDomain.
            // That's why we loop here instead of just return back to the caller.
            // This is purely custom behavior, change in your EntryPoint as you see fit.
            while (!cancellationToken.IsCancellationRequested)
            {
                _cancelledOrSettingsChangedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                runOnce((observer, services) =>
                    {
                        Notify(observer, () => new RuntimeStartedEvent(environment.Host));
                        var scheduler = new Scheduler(services, observer);
                        try
                        {
                            scheduler.RunSchedule(_cancelledOrSettingsChangedCts.Token);
                        }
                        catch (ThreadInterruptedException)
                        {
                            Notify(observer, () => new RuntimeInterruptedRestartedEvent(environment.Host, GetNameOfServiceInExecution(scheduler)));
                        }
                        catch (ThreadAbortException)
                        {
                            Thread.ResetAbort();
                            Notify(observer, () => new RuntimeInterruptedRestartedEvent(environment.Host, GetNameOfServiceInExecution(scheduler)));
                        }
                        catch (TimeoutException)
                        {
                            Notify(observer, () => new RuntimeTimeoutRestartedEvent(environment.Host, GetNameOfServiceInExecution(scheduler)));
                        }
                        catch (Exception ex)
                        {
                            Notify(observer, () => new RuntimeExceptionRestartedEvent(environment.Host, GetNameOfServiceInExecution(scheduler), ex));
                        }
                        finally
                        {
                            Notify(observer, () => new RuntimeStoppedEvent(environment.Host));
                        }
                    });
            }
        }

        public void OnSettingsChanged()
        {
            var cts = _cancelledOrSettingsChangedCts;
            if (cts != null)
            {
                cts.Cancel();
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

        private void Notify(IRuntimeObserver observer, Func<IRuntimeEvent> buildEvent)
        {
            if (observer != null)
            {
                observer.Notify(buildEvent());
            }
        }
    }
}
