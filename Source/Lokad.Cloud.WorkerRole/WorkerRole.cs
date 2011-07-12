#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Cloud.Services.Framework.Instrumentation;
using Lokad.Cloud.Services.Framework.Logging;
using Lokad.Cloud.Storage;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Lokad.Cloud.Worker
{
    /// <summary>Entry point of Lokad.Cloud.</summary>
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Services.Runtime.Runtime _runtime;
        private Task _runtimeTask;

        public WorkerRole()
        {
            var storage = CloudStorage
                .ForAzureConnectionString(RoleEnvironment.GetConfigurationSettingValue("DataConnectionString"))
                .BuildStorageProviders();

            _cancellationTokenSource = new CancellationTokenSource();

            var log = new CloudLogWriter(storage);
            var observer = new CloudRuntimeInstrumentationSubject();

            // TODO: more sensible subscriptions (with text, filtered, throttled)
            observer.Subscribe(@event => log.DebugFormat("Runtime: {0}", @event.ToString()));

            _runtime = new Services.Runtime.Runtime(storage, observer);
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
            var task = _runtime.Run(_cancellationTokenSource.Token);
            _runtimeTask = task;

            task.Wait();
        }
    }
}
