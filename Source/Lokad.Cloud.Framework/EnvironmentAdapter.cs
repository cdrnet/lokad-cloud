#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Lokad.Cloud
{
    /// <summary>
    /// Intermediate CloudEnvironment to IEnvironment adapter.
    /// </summary>
    [Obsolete("To be removed after the transition away from statis CloudEnvironment is completed.")]
    public class EnvironmentAdapter : MarshalByRefObject, IEnvironment
    {
        static bool _runtimeAvailable;
        private readonly string _workerName = Dns.GetHostName();

        public HostInfo Host
        {
            get { return new HostInfo(_workerName, "Primary", "Lokad.Cloud"); }
        }

        public string GetSettingValue(string settingName)
        {
            if (!IsAvailable)
            {
                return null;
            }

            try
            {
                var value = RoleEnvironment.GetConfigurationSettingValue(settingName);
                if (!String.IsNullOrEmpty(value))
                {
                    value = value.Trim();
                }

                return String.IsNullOrEmpty(value) ? null : value;
            }
            catch (RoleEnvironmentException)
            {
                // e.g. setting was removed from the csdef
                return null;
            }
        }

        public X509Certificate2 GetCertificate(string thumbprint)
        {
            var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            try
            {
                store.Open(OpenFlags.ReadOnly);
                return store.Certificates
                    .Find(X509FindType.FindByThumbprint, thumbprint, false)
                    .OfType<X509Certificate2>()
                    .FirstOrDefault();
            }
            finally
            {
                store.Close();
            }
        }

        public string GetLocalResourcePath(string resourceName)
        {
            if (IsAvailable)
            {
                return RoleEnvironment.GetLocalResource(resourceName).RootPath;
            }

            var dir = Path.Combine(Path.GetTempPath(), _workerName, resourceName);
            Directory.CreateDirectory(dir);
            return dir;
        }

        public IPEndPoint GetEndpoint(string endpointName)
        {
            if (!IsAvailable)
            {
                return null;
            }

            RoleInstanceEndpoint endpoint;
            return RoleEnvironment.CurrentRoleInstance.InstanceEndpoints.TryGetValue(endpointName, out endpoint) ? endpoint.IPEndpoint : null;
        }

        static bool IsAvailable
        {
            get
            {
                if (!_runtimeAvailable)
                {
                    // try again, maybe it was not available the last time but now is.
                    try
                    {
                        _runtimeAvailable = RoleEnvironment.IsAvailable;
                    }
                    catch (TypeInitializationException)
                    {
                        _runtimeAvailable = false;
                    }
                }

                return _runtimeAvailable;
            }
        }
    }
}
