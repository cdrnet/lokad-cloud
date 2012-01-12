#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;
using System.Reflection;
using System.Security;
using System.Threading;
using System.Xml.Linq;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.AppHost.Framework.Definition;
using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.Runtime;
using Lokad.Cloud.ServiceFabric.Runtime;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.Host
{
    /// <summary>
    /// AppDomain-isolated host for a single runtime instance.
    /// </summary>
    internal class IsolatedSingleRuntimeHost
    {
        private readonly HostContext _hostContext;

        /// <summary>Refer to the callee instance (isolated). This property is not null
        /// only for the caller instance (non-isolated).</summary>
        volatile SingleRuntimeHost _isolatedInstance;

        public IsolatedSingleRuntimeHost(HostContext hostContext)
        {
            _hostContext = hostContext;
        }

        /// <summary>
        /// Run the hosted runtime, blocking the calling thread.
        /// </summary>
        /// <returns>True if the worker stopped as planned (e.g. due to updated assemblies)</returns>
        public bool Run()
        {
            var settings = CloudConfigurationSettings.LoadFromRoleEnvironment();

            // The trick is to load this same assembly in another domain, then
            // instantiate this same class and invoke Run
            var domain = AppDomain.CreateDomain("WorkerDomain", null, AppDomain.CurrentDomain.SetupInformation);

            bool restartForAssemblyUpdate;

            try
            {
                _isolatedInstance = (SingleRuntimeHost)domain.CreateInstanceAndUnwrap(
                    Assembly.GetExecutingAssembly().FullName,
                    typeof(SingleRuntimeHost).FullName);

                var solution = new SolutionHead("Solution");
                var assemblies = new AssembliesHead("Assemblies");
                var environment = new ApplicationEnvironment(
                    _hostContext,
                    _hostContext.GetNewCellLifeIdentity("Lokad.Cloud", "Cell", solution),
                    solution,
                    assemblies,
                    cmd => { });

                // This never throws, unless something went wrong with IoC setup and that's fine
                // because it is not possible to execute the worker
                restartForAssemblyUpdate = _isolatedInstance.Run(settings, _hostContext.DeploymentReader, environment);
            }
            finally
            {
                _isolatedInstance = null;

                // If this throws, it's because something went wrong when unloading the AppDomain
                // The exception correctly pulls down the entire worker process so that no AppDomains are
                // left in memory
                AppDomain.Unload(domain);
            }

            return restartForAssemblyUpdate;
        }

        /// <summary>
        /// Immediately stop the runtime host and wait until it has exited (or a timeout expired).
        /// </summary>
        public void Stop()
        {
            var instance = _isolatedInstance;
            if (null != instance)
            {
                _isolatedInstance.Stop();
            }
        }
    }

    /// <summary>
    /// Host for a single runtime instance.
    /// </summary>
    internal class SingleRuntimeHost : MarshalByRefObject, IDisposable
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
                var cellSettings = new XElement("Settings",
                    new XElement("DataConnectionString", settings.DataConnectionString),
                    new XElement("CertificateThumbprint", settings.SelfManagementCertificateThumbprint),
                    new XElement("SubscriptionId", settings.SelfManagementSubscriptionId));

                var entryPoint = new EntryPoint.ApplicationEntryPoint();

                // runtime endlessly keeps pinging queues for pending work
                entryPoint.Run(cellSettings, deploymentReader, environment, _cancellationTokenSource.Token);

                log.DebugFormat("Runtime Host: Runtime has stopped cleanly on worker {0}.",
                    environment.Host.WorkerName);
            }
            catch (TypeLoadException typeLoadException)
            {
                log.ErrorFormat(typeLoadException, "Runtime Host: Type {0} could not be loaded. The Runtime Host will be restarted.",
                    typeLoadException.TypeName);
            }
            catch (FileLoadException fileLoadException)
            {
                // Tentatively: referenced assembly is missing
                log.Fatal(fileLoadException, "Runtime Host: Could not load assembly probably due to a missing reference assembly. The Runtime Host will be restarted.");
            }
            catch (SecurityException securityException)
            {
                // Tentatively: assembly cannot be loaded due to security config
                log.FatalFormat(securityException, "Runtime Host: Could not load assembly {0} probably due to security configuration. The Runtime Host will be restarted.",
                    securityException.FailedAssemblyInfo);
            }
            catch (TriggerRestartException)
            {
                log.DebugFormat("Runtime Host: Triggered to stop execution on worker {0}. The Role Instance will be recycled and the Runtime Host restarted.",
                    environment.Host.WorkerName);

                return true;
            }
            catch (ThreadAbortException)
            {
                Thread.ResetAbort();
                log.DebugFormat("Runtime Host: execution was aborted on worker {0}. The Runtime is stopping.", environment.Host.WorkerName);
            }
            catch (Exception ex)
            {
                // Generic exception
                log.ErrorFormat(ex, "Runtime Host: An unhandled {0} exception occurred on worker {1}. The Runtime Host will be restarted.",
                    ex.GetType().Name, environment.Host.WorkerName);
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
