#region Copyright (c) Lokad 2010-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Cloud.Provisioning;
using Lokad.Cloud.Provisioning.Instrumentation;

namespace Lokad.Cloud.Services.AppContext
{
    /// <summary>Azure Management API Provider, Provisioning Provider.</summary>
    internal class CloudProvisioning
    {
        private readonly AzureCurrentDeployment _currentDeployment;
        private readonly AzureProvisioning _provisioning;

        /// <summary>IoC constructor.</summary>
        public CloudProvisioning(string subscriptionId, string deploymentId, X509Certificate2 certificate, ICloudProvisioningObserver observer)
        {
            // skip further processing on intrinsic fault states
            if (string.IsNullOrWhiteSpace(deploymentId) || deploymentId.StartsWith("deployment(")
                || string.IsNullOrWhiteSpace(subscriptionId) || certificate == null)
            {
                return;
            }

            // ok
            _provisioning = new AzureProvisioning(subscriptionId, certificate, observer);
            _currentDeployment = new AzureCurrentDeployment(deploymentId, subscriptionId, certificate, observer);

            _currentDeployment.Discover(CancellationToken.None).ContinueWith(t =>
                {
                    var baseException = t.Exception.GetBaseException();

                    if (ProvisioningErrorHandling.IsTransientError(baseException))
                    {
                        //_log.DebugFormat(baseException, "Provisioning: Initial discovery failed with a transient error.");
                        return;
                    }

                    HttpStatusCode httpStatus;
                    if (ProvisioningErrorHandling.TryGetHttpStatusCode(baseException, out httpStatus))
                    {
                        switch(httpStatus)
                        {
                            case HttpStatusCode.Forbidden:
                                //_log.WarnFormat(baseException, "Provisioning: Initial discovery failed with HTTP 403 Forbidden. We tried using subscription '{0}' and certificate '{1}' ({2}) {3} a private key.",
                                //    subscriptionId, certificate.SubjectName.Name, certificate.Thumbprint, certificate.HasPrivateKey ? "with" : "without");
                                return;
                            default:
                                //_log.WarnFormat(baseException, "Provisioning: Initial discovery failed with a permanent HTTP {0} {1} error.", (int)httpStatus, httpStatus);
                                return;
                        }
                    }

                    //_log.WarnFormat(baseException, "Provisioning: Initial discovery failed with a permanent error.");
                }, TaskContinuationOptions.OnlyOnFaulted);
        }

        public bool IsAvailable
        {
            get { return _provisioning != null; }
        }

        /// <remarks>
        /// Logs exceptions, hence failing to handle a task fault at the calling side
        /// will not cause an unhandled exception at finalization
        /// </remarks>
        public Task SetWorkerInstanceCount(int count, CancellationToken cancellationToken)
        {
            if (count <= 0 && count > 500)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            //_log.InfoFormat("Provisioning: Updating the worker instance count to {0}.", count);

            var task = _provisioning.UpdateCurrentLokadCloudWorkerCount(_currentDeployment, count, cancellationToken);

            // TODO (ruegg, 2011-05-30): Replace with system events
            task.ContinueWith(t =>
                {
                    try
                    {
                        if (t.IsFaulted)
                        {
                            HttpStatusCode httpStatus;
                            if (ProvisioningErrorHandling.TryGetHttpStatusCode(t.Exception, out httpStatus))
                            {
                                switch(httpStatus)
                                {
                                    case HttpStatusCode.Conflict:
                                        //_log.DebugFormat("Provisioning: Updating the worker instance count to {0} failed because another deployment update is already in progress.", count);
                                        break;
                                    default:
                                        //_log.DebugFormat("Provisioning: Updating the worker instance count failed with HTTP Status {0} ({1}).", httpStatus, (int)httpStatus);
                                        break;
                                }
                            }
                            else if (ProvisioningErrorHandling.IsTransientError(t.Exception))
                            {
                                //_log.DebugFormat(task.Exception.GetBaseException(), "Provisioning: Updating the worker instance count failed with a transient error.");
                            }
                            else
                            {
                                //_log.WarnFormat(task.Exception.GetBaseException(), "Provisioning: Updating the worker instance count failed with a permanent error.");
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // We don't really care, it's only logging that failed
                    }
                }, TaskContinuationOptions.ExecuteSynchronously);

            return task;
        }
    }
}