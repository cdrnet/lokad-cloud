#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Security.Cryptography;

namespace Lokad.Cloud.Services.Management.Deployments
{
    internal class ContentBasedNaming
    {
        private const string Extension = ".lokadcloud";
        private const string AssembliesPrefix = "assemblies-";
        private const string ConfigPrefix = "config-";
        private const string SettingsPrefix = "settings-";
        private const string DeploymentPrefix = "deployment-";

        private readonly HashAlgorithm _hashAlgorithm;

        public ContentBasedNaming()
        {
            _hashAlgorithm = SHA256.Create();
            _hashAlgorithm.Initialize();
        }

        public string NameForAssemblies(byte[] assembliesZipBytes)
        {
            return string.Concat(AssembliesPrefix, SafeHashString(assembliesZipBytes), Extension);
        }

        public string NameForConfig(byte[] configBytes)
        {
            return string.Concat(ConfigPrefix, SafeHashString(configBytes), Extension);
        }

        public string NameForSettings(byte[] settingsBytes)
        {
            return string.Concat(SettingsPrefix, SafeHashString(settingsBytes), Extension);
        }

        public string NameForDeployment(byte[] settingsBytes)
        {
            return string.Concat(DeploymentPrefix, SafeHashString(settingsBytes), Extension);
        }

        string SafeHashString(byte[] data)
        {
            var hash = _hashAlgorithm.ComputeHash(data);
            return Convert.ToBase64String(hash).Replace('/', '_').Replace('+', '-').Replace('=', '$');
        }
    }
}
