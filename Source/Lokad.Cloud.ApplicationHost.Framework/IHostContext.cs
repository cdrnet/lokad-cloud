#region Copyright (c) Lokad 2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Security.Cryptography.X509Certificates;

namespace Lokad.Cloud.AppHost.Framework
{
    /// <remarks>Implementation does not need to be serializable (other than the deployment reader).</remarks>
    public interface IHostContext
    {
        IDeploymentReader DeploymentReader { get; }
        int CurrentWorkerInstanceCount { get; }
        string GetConfigurationSettingValue(string settingName);
        X509Certificate2 GetConfigurationCertificate(string thumbprint);
    }
}
