using System;
using System.Reflection;
using Lokad.Cloud.AppHost.Framework.Definition;

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
        volatile AppDomainEntryPoint _isolatedInstance;

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
                _isolatedInstance = (AppDomainEntryPoint)domain.CreateInstanceAndUnwrap(
                    Assembly.GetExecutingAssembly().FullName,
                    typeof(AppDomainEntryPoint).FullName);

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
}