#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace Lokad.Cloud
{
    public interface IEnvironment
    {
        HostInfo Host { get; }

        string GetSettingValue(string settingName);
        X509Certificate2 GetCertificate(string thumbprint);
        string GetLocalResourcePath(string resourceName);
        IPEndPoint GetEndpoint(string endpointName);

        int CurrentWorkerInstanceCount { get; }
        void ProvisionWorkerInstances(int numberOfInstances);
        void ProvisionWorkerInstancesAtLeast(int minNumberOfInstances);
    }

    public class HostInfo
    {
        public string SolutionName { get; private set; }
        public string WorkerName { get; private set; }
        public string CellName { get; private set; }

        public HostInfo(string worker, string cell, string solution)
        {
            WorkerName = worker;
            CellName = cell;
            SolutionName = solution;
        }
    }
}
