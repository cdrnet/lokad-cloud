#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Net;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Cloud.AppHost;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.Provisioning.Instrumentation;
using Lokad.Cloud.Provisioning.Instrumentation.Events;
using Lokad.Cloud.Services.AppContext;
using Lokad.Cloud.Services.Framework.Logging;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Lokad.Cloud.Worker
{
    /// <summary>Entry point of Lokad.Cloud.</summary>
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Host _host;
        private Task _runtimeTask;

        public WorkerRole()
        {
            _cancellationTokenSource = new CancellationTokenSource();

            var context = new HostContext();
            context.Observer = BuildHostObserver(context.Log);
            context.ProvisioningObserver = BuildProvisioningObserver(context.Log);

            _host = new Host(context);
        }

        static IHostObserver BuildHostObserver(ILogWriter log)
        {
            var subject = new HostObserverSubject();

            // TODO: more sensible subscriptions (with text, filtered, throttled)
            subject.Subscribe(@event => log.DebugFormat("Runtime: {0}", @event.ToString()));

            return subject;
        }

        static ICloudProvisioningObserver BuildProvisioningObserver(ILogWriter log)
        {
            var hostName = Dns.GetHostName();
            var subject = new CloudProvisioningInstrumentationSubject();

            subject.OfType<ProvisioningOperationRetriedEvent>()
                .Throttle(TimeSpan.FromMinutes(5))
                .Subscribe(@event => log.DebugFormat(@event.Exception, "Provisioning: Retried for the {1} policy on {2}: {3}",
                    @event.Policy, hostName, (@event.Exception != null) ? @event.Exception.Message : "reason unknown"));

            return subject;
        }

        /// <summary>
        /// Called by Windows Azure when the role instance is to be stopped. 
        /// </summary>
        /// <remarks>
        /// <para>
        /// Override the OnStop method to implement any code your role requires to
        /// shut down in an orderly fashion.
        /// </para>
        /// <para>
        /// This method must return within certain period of time. If it does not,
        /// Windows Azure will stop the role instance.
        /// </para>
        /// <para>
        /// A web role can include shutdown sequence code in the ASP.NET
        /// Application_End method instead of the OnStop method. Application_End is
        /// called before the Stopping event is raised or the OnStop method is called.
        /// </para>
        /// <para>
        /// Any exception that occurs within the OnStop method is an unhandled
        /// exception.
        /// </para>
        /// </remarks>
        public override void OnStop()
        {
            _cancellationTokenSource.Cancel();

            var task = _runtimeTask;
            if (task != null)
            {
                task.Wait(TimeSpan.FromMinutes(5));
            }
        }

        /// <summary>
        /// Called by Windows Azure after the role instance has been initialized. This
        /// method serves as the main thread of execution for your role.
        /// </summary>
        /// <remarks>
        /// <para>The role recycles when the Run method returns.</para>
        /// <para>Any exception that occurs within the Run method is an unhandled exception.</para>
        /// </remarks>
        public override void Run()
        {
            var task = _host.Run(_cancellationTokenSource.Token);
            _runtimeTask = task;

            task.Wait();
        }
    }
}
