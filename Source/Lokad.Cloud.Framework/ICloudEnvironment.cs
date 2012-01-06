#region Copyright (c) Lokad 2010-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Security.Cryptography.X509Certificates;

namespace Lokad.Cloud
{
    public interface ICloudEnvironment
    {
        string WorkerName { get; }
        string UniqueWorkerInstanceName { get; }

        int CurrentWorkerInstanceCount { get; }
        void ProvisionWorkerInstances(int numberOfInstances);
        void ProvisionWorkerInstancesAtLeast(int minNumberOfInstances);

        string GetSettingValue(string settingName);
        X509Certificate2 GetCertificate(string thumbprint);
        string GetLocalResourcePath(string resourceName);
    }
}
