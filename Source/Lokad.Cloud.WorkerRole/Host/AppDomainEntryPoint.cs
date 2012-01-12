#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Threading;
using System.Xml.Linq;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.Runtime;
using Lokad.Cloud.ServiceFabric.Runtime;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.Host
{
    /// <summary>
    /// Host for a single runtime instance.
    /// </summary>
    internal class AppDomainEntryPoint : MarshalByRefObject, IDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly EventWaitHandle _stoppedWaitHandle = new ManualResetEvent(false);

        /// <summary>
        /// Run the hosted runtime, blocking the calling thread.
        /// </summary>
        /// <returns>True if the worker stopped as planned (e.g. due to updated assemblies)</returns>
        public bool Run(CloudConfigurationSettings settings, IDeploymentReader deploymentReader, ApplicationEnvironment environment)
        {
            _stoppedWaitHandle.Reset();

            var log = new CloudLogWriter(CloudStorage.ForAzureConnectionString(settings.DataConnectionString).BuildBlobStorage());

            AppDomain.CurrentDomain.UnhandledException += (sender, e) => log.ErrorFormat(
                e.ExceptionObject as Exception,
                "Runtime Host: An unhandled {0} exception occurred on worker {1} in a background thread. The Runtime Host will be restarted: {2}.",
                e.ExceptionObject.GetType().Name, environment.Host.WorkerName, e.IsTerminating);

            try
            {
                var runtimeProviders = CloudStorage
                    .ForAzureConnectionString(settings.DataConnectionString)
                    .WithObserver(Observers.CreateStorageObserver(log))
                    .BuildRuntimeProviders(log);

                var assemblyLoader = new AssemblyLoader(runtimeProviders);
                assemblyLoader.LoadPackage();

                var cellSettings = new XElement("Settings",
                    new XElement("DataConnectionString", settings.DataConnectionString),
                    new XElement("CertificateThumbprint", settings.SelfManagementCertificateThumbprint),
                    new XElement("SubscriptionId", settings.SelfManagementSubscriptionId));

                var entryPoint = new EntryPoint.ApplicationEntryPoint();

                // runtime endlessly keeps pinging queues for pending work
                entryPoint.Run(cellSettings, deploymentReader, environment, _cancellationTokenSource.Token);
            }
            catch (TriggerRestartException)
            {
                return true;
            }
            finally
            {
                _stoppedWaitHandle.Set();
            }

            return false;
        }

        /// <summary>
        /// Immediately stop the runtime host and wait until it has exited (or a timeout expired).
        /// </summary>
        public void Stop()
        {
            _cancellationTokenSource.Cancel();

            // note: we DO have to wait until the shut down has finished,
            // or the Azure Fabric will tear us apart early!
            _stoppedWaitHandle.WaitOne(TimeSpan.FromSeconds(25));
        }

        public void Dispose()
        {
            _stoppedWaitHandle.Close();
        }
    }
}
