#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Security.Cryptography.X509Certificates;
using Lokad.Cloud.AppHost.Framework;

namespace Lokad.Cloud
{
    /// <summary>
    /// Cloud Environment
    /// </summary>
    /// <remarks>
    /// See also AppHost IApplicationEnvironment
    /// </remarks>
    public sealed class CloudEnvironment : ICloudEnvironment
    {
        private readonly IApplicationEnvironment _appEnvironment;

        public CloudEnvironment(IApplicationEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
        }

        public string WorkerName
        {
            get { return _appEnvironment.Host.WorkerName; }
        }

       public string UniqueWorkerInstanceName
        {
            get { return _appEnvironment.Host.UniqueWorkerInstanceName; }
        }

        public int CurrentWorkerInstanceCount
        {
            get { return _appEnvironment.CurrentWorkerInstanceCount; }
        }

        public void ProvisionWorkerInstances(int numberOfInstances)
        {
            _appEnvironment.ProvisionWorkerInstances(numberOfInstances);
        }

        public void ProvisionWorkerInstancesAtLeast(int minNumberOfInstances)
        {
            _appEnvironment.ProvisionWorkerInstancesAtLeast(minNumberOfInstances);
        }

        public string GetSettingValue(string settingName)
        {
            return _appEnvironment.GetSettingValue(settingName);
        }

        public X509Certificate2 GetCertificate(string thumbprint)
        {
            return _appEnvironment.GetCertificate(thumbprint);
        }

        public string GetLocalResourcePath(string resourceName)
        {
            return _appEnvironment.GetLocalResourcePath(resourceName);
        }
    }
}