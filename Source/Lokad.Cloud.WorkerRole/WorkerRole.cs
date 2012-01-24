#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Linq;
using System.Threading;
using Lokad.Cloud.AppHost;
using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.Storage;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Lokad.Cloud
{
    /// <summary>Entry point of Lokad.Cloud.</summary>
    public class WorkerRole : RoleEntryPoint
    {
        private CancellationTokenSource _cancellationTokenSource;
        private Host _host;

        /// <summary>
        /// Called by Windows Azure to initialize the role instance.
        /// </summary>
        /// <returns>
        /// True if initialization succeeds, False if it fails. The default implementation returns True.
        /// </returns>
        /// <remarks>
        /// <para>Any exception that occurs within the OnStart method is an unhandled exception.</para>
        /// </remarks>
        public override bool OnStart()
        {
            if (!RoleEnvironment.IsAvailable)
            {
                return false;
            }

            var connectionString = RoleEnvironment.GetConfigurationSettingValue("DataConnectionString");
            var subscriptionId = RoleEnvironment.GetConfigurationSettingValue("SelfManagementSubscriptionId");
            var certificateThumbprint = RoleEnvironment.GetConfigurationSettingValue("SelfManagementCertificateThumbprint");
            var deploymentReader = new DeploymentReader(connectionString, subscriptionId, certificateThumbprint);

            var log = new HostLogWriter(CloudStorage.ForAzureConnectionString(connectionString).BuildBlobStorage());
            var context = new HostContext(deploymentReader, certificateThumbprint, subscriptionId, log);

            _cancellationTokenSource = new CancellationTokenSource();
            _host = new Host(context);

            RoleEnvironment.Changing += OnRoleEnvironmentChanging;

            return true;
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
            RoleEnvironment.Changing -= OnRoleEnvironmentChanging;

            _cancellationTokenSource.Cancel(true);
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
            _host.RunSync(_cancellationTokenSource.Token);
        }

        void OnRoleEnvironmentChanging(object sender, RoleEnvironmentChangingEventArgs e)
        {
            // We restart all workers if the configuration changed (e.g. the storage account)
            // We do not request a recycle if only the topology changed, e.g. if some instances have been removed or added.
            if (e.Changes.OfType<RoleEnvironmentConfigurationSettingChange>().Any())
            {
                _cancellationTokenSource.Cancel();
                RoleEnvironment.RequestRecycle();
            }
        }
    }
}
