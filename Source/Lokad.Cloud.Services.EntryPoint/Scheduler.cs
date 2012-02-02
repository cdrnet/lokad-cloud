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
    /// Round robin scheduler with adaptive modifications: tasks that claim to have
    /// more work ready are given the chance to continue until they reach a fixed
    /// time limit (greedy), and the scheduling is slowed down when all available
    /// services skip execution consecutively.
    /// </summary>
    internal class Scheduler
    {
        readonly IRuntimeObserver _observer;

        readonly List<CloudService> _services;
        readonly Func<CloudService, ServiceExecutionFeedback> _schedule;

        /// <summary>Duration to keep pinging the same cloud service if service is active.</summary>
        readonly TimeSpan _moreOfTheSame = TimeSpan.FromSeconds(60);

        /// <summary>Resting duration.</summary>
        readonly TimeSpan _idleSleep = TimeSpan.FromSeconds(10);

        CloudService _currentService;

        /// <summary>
        /// Creates a new instance of the Scheduler class.
        /// </summary>
        /// <param name="services">cloud services</param>
        /// <param name="schedule">Action to be invoked when a service is scheduled to run</param>
        public Scheduler(List<CloudService> services, Func<CloudService, ServiceExecutionFeedback> schedule, IRuntimeObserver observer = null)
        {
            _observer = observer;
            _services = services;
            _schedule = schedule;
        }

        public CloudService CurrentlyScheduledService
        {
            get { return _currentService; }
        }

        public void RunSchedule(CancellationToken cancellationToken)
        {
            var currentThread = Thread.CurrentThread;
            var services = _services;
            var currentServiceIndex = -1;
            var skippedConsecutively = 0;

            if (_observer != null)
            {
                _observer.Notify(new SchedulerBusyEvent(DateTimeOffset.UtcNow));
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                currentServiceIndex = (currentServiceIndex + 1) % services.Count;
                _currentService = services[currentServiceIndex];

                var result = ServiceExecutionFeedback.DontCare;
                var isRunOnce = false;

                // 'more of the same pattern'
                // as long the service is active, keep triggering the same service
                // for at least 1min (in order to avoid a single service to monopolize CPU)
                var start = DateTimeOffset.UtcNow;
                
                using(cancellationToken.Register(currentThread.Abort))
                while (DateTimeOffset.UtcNow.Subtract(start) < _moreOfTheSame && !cancellationToken.IsCancellationRequested && DemandsImmediateStart(result))
                {
                    result = _schedule(_currentService);
                    isRunOnce |= WasSuccessfullyExecuted(result);
                }

                skippedConsecutively = isRunOnce ? 0 : skippedConsecutively + 1;
                if (skippedConsecutively >= services.Count && !cancellationToken.IsCancellationRequested)
                {
                    // We are not using 'Thread.Sleep' because we want the worker
                    // to terminate fast if 'Stop' is requested.

                    if (_observer != null)
                    {
                        _observer.Notify(new SchedulerIdleEvent(DateTimeOffset.UtcNow));
                    }

                    cancellationToken.WaitHandle.WaitOne(_idleSleep);

                    if (_observer != null)
                    {
                        _observer.Notify(new SchedulerBusyEvent(DateTimeOffset.UtcNow));
                    }

                    skippedConsecutively = 0;
                }
            }

            _currentService = null;
        }

        /// <summary>
        /// The service was successfully executed and it might make sense to execute
        /// it again immediately (greedy).
        /// </summary>
        bool DemandsImmediateStart(ServiceExecutionFeedback feedback)
        {
            return feedback == ServiceExecutionFeedback.WorkAvailable
                || feedback == ServiceExecutionFeedback.DontCare;
        }

        /// <summary>
        /// The service was actually executed (not skipped) and did not fail.
        /// </summary>
        bool WasSuccessfullyExecuted(ServiceExecutionFeedback feedback)
        {
            return feedback != ServiceExecutionFeedback.Skipped
                && feedback != ServiceExecutionFeedback.Failed;
        }
    }
}
