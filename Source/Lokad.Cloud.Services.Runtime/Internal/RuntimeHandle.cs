#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Net;
using Lokad.Cloud.Services.Framework.Commands;
using Lokad.Cloud.Services.Framework.Instrumentation;
using Lokad.Cloud.Services.Framework.Instrumentation.Events;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Lokad.Cloud.Services.Runtime.Internal
{
    /// <summary>
    /// Handle to communicate with the runtime from the outside:
    /// - Send commands to the runtime
    /// - Report (stale) info about the runtime to the outside (like current deployment)
    /// </summary>
    internal class RuntimeHandle
    {
        private readonly ICloudRuntimeObserver _observer;

        internal readonly Action<ICloudCommand> SendCommand;
        internal readonly Lazy<string> MachineName = new Lazy<string>(Dns.GetHostName);

        internal string ApplicationDeploymentName { get; set; }

        internal RuntimeHandle(Action<ICloudCommand> sendCommand, ICloudRuntimeObserver observer)
        {
            _observer = observer;
            SendCommand = sendCommand;
        }

        internal string AzureDeploymentId
        {
            get { return RoleEnvironment.DeploymentId; }
        }

        internal int WorkerInstanceCount
        {
            get { return RoleEnvironment.CurrentRoleInstance.Role.Instances.Count; }
        }

        public void TryNotify(Func<ICloudRuntimeEvent> @event)
        {
            // TODO: Consider to drop from handle again, is not really a runtime concern

            if (_observer != null)
            {
                try
                {
                    _observer.Notify(@event());
                }
                catch
                {
                    // Suppression is intended: we can't log but also don't want to tear down just because of a failed notification
                }
            }
        }
    }
}
