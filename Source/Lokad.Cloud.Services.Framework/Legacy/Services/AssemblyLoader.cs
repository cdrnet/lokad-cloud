#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.Runtime
{
    /// <remarks>
    /// Since the assemblies are loaded in the current <c>AppDomain</c>, this class
    /// should be a natural candidate for a singleton design pattern. Yet, keeping
    /// it as a plain class facilitates the IoC instantiation.
    /// </remarks>
    internal class AssemblyLoader
    {
        public const string AssembliesContainerName = "lokad-cloud-assemblies";
        public const string PackageBlobName = "default";
        public const string ConfigurationBlobName = "config";

        /// <summary>Frequency for checking for update concerning the assembly package.</summary>
        public static TimeSpan UpdateCheckFrequency
        {
            get { return TimeSpan.FromMinutes(1); }
        }

        readonly IBlobStorageProvider _blobs;

        /// <summary>Etag of the assembly package. This property is set when
        /// assemblies are loaded. It can be used to monitor the availability of
        /// a new package.</summary>
        string _lastPackageEtag;

        string _lastConfigurationEtag;

        DateTimeOffset _lastPackageCheck;

        /// <summary>Build a new package loader.</summary>
        public AssemblyLoader(IBlobStorageProvider blobStorageProvider)
        {
            _blobs = blobStorageProvider;
        }

        /// <summary>
        /// Reset the update status to the currently available version,
        /// such that <see cref="CheckUpdate"/> does not cause an update to happen.
        /// </summary>
        public void ResetUpdateStatus()
        {
            _lastPackageEtag = _blobs.GetBlobEtag(AssembliesContainerName, PackageBlobName);
            _lastConfigurationEtag = _blobs.GetBlobEtag(AssembliesContainerName, ConfigurationBlobName);
            _lastPackageCheck = DateTimeOffset.UtcNow;
        }

        /// <summary>Check for the availability of a new assembly package
        /// and throw a <see cref="TriggerRestartException"/> if a new package
        /// is available.</summary>
        /// <param name="delayCheck">If <c>true</c> then the actual update
        /// check if performed not more than the frequency specified by 
        /// <see cref="UpdateCheckFrequency"/>.</param>
        public void CheckUpdate(bool delayCheck)
        {
            var now = DateTimeOffset.UtcNow;

            // limiting the frequency where the actual update check is performed.
            if (delayCheck && now.Subtract(_lastPackageCheck) <= UpdateCheckFrequency)
            {
                return;
            }

            var newPackageEtag = _blobs.GetBlobEtag(AssembliesContainerName, PackageBlobName);
            var newConfigurationEtag = _blobs.GetBlobEtag(AssembliesContainerName, ConfigurationBlobName);

            if (!string.Equals(_lastPackageEtag, newPackageEtag))
            {
                throw new TriggerRestartException("Assemblies update has been detected.");
            }

            if (!string.Equals(_lastConfigurationEtag, newConfigurationEtag))
            {
                throw new TriggerRestartException("Configuration update has been detected.");
            }
        }
    }
}