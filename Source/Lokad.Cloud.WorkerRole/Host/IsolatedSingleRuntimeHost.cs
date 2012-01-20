using System;
using System.Linq;
using System.Reflection;

namespace Lokad.Cloud.Host
{
    /// <summary>
    /// AppDomain-isolated host for a single runtime instance.
    /// </summary>
    internal class IsolatedSingleRuntimeHost
    {
        private readonly HostContext _hostContext;
        volatile AppDomainEntryPoint _appDomainEntryPoint;

        public IsolatedSingleRuntimeHost(HostContext hostContext)
        {
            _hostContext = hostContext;
        }

        /// <summary>
        /// Run the hosted runtime, blocking the calling thread.
        /// </summary>
        /// <returns>True if the worker stopped as planned (e.g. due to updated assemblies)</returns>
        public void Run()
        {
            var domain = AppDomain.CreateDomain("WorkerDomain", null, AppDomain.CurrentDomain.SetupInformation);
            try
            {
                _appDomainEntryPoint = (AppDomainEntryPoint)domain.CreateInstanceAndUnwrap(
                    Assembly.GetExecutingAssembly().FullName,
                    typeof(AppDomainEntryPoint).FullName);

                var deploymentReader = _hostContext.DeploymentReader;

                string etag;
                var deployment = deploymentReader.GetDeploymentIfModified(null, out etag);
                var solution = deploymentReader.GetSolution(deployment);
                var cell = solution.Cells.Single();

                var environment = new ApplicationEnvironment(
                    _hostContext,
                    _hostContext.GetNewCellLifeIdentity(solution.SolutionName, cell.CellName, deployment),
                    deployment,
                    cell.Assemblies,
                    cmd => { });

                // This never throws, unless something went wrong with IoC setup and that's fine
                // because it is not possible to execute the worker
                _appDomainEntryPoint.Run(cell, deploymentReader, environment);
            }
            finally
            {
                _appDomainEntryPoint = null;

                // If this throws, it's because something went wrong when unloading the AppDomain
                // The exception correctly pulls down the entire worker process so that no AppDomains are
                // left in memory
                AppDomain.Unload(domain);
            }
        }

        /// <summary>
        /// Immediately stop the runtime host and wait until it has exited (or a timeout expired).
        /// </summary>
        public void Stop()
        {
            var instance = _appDomainEntryPoint;
            if (null != instance)
            {
                _appDomainEntryPoint.Stop();
            }
        }
    }
}