#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Autofac;

namespace Lokad.Cloud
{
    /// <summary>
    /// IoC configuration module for Azure storage and management credentials.
    /// Intended mostly for loading via appconfig, else it's simpler to register a
    /// <see cref="CloudConfigurationSettings"/> instance directly.
    /// </summary>
    /// <remarks>
    /// When only using the storage (O/C mapping) toolkit standalone it is easier
    /// to let the <see cref="Storage.CloudStorage"/> factory create the storage providers on demand.
    /// </remarks>
    /// <seealso cref="Storage.CloudStorage"/>
    public sealed class CloudConfigurationModule : Module
    {
        /// <summary>Azure storage connection string.</summary>
        public string DataConnectionString { get; set; }

        /// <summary>Azure subscription Id (optional).</summary>
        public string SelfManagementSubscriptionId { get; set; }

        /// <summary>Azure management certificate thumbprint (optional).</summary>
        public string SelfManagementCertificateThumbprint { get; set; }

        protected override void Load(ContainerBuilder builder)
        {
            var settings = new CloudConfigurationSettings
                {
                    DataConnectionString = DataConnectionString,
                    SelfManagementSubscriptionId = SelfManagementSubscriptionId,
                    SelfManagementCertificateThumbprint = SelfManagementCertificateThumbprint
                };

            if (string.IsNullOrEmpty(settings.DataConnectionString))
            {
                settings = CloudConfigurationSettings.LoadFromRoleEnvironment();
            }

            // Only register storage components if the storage credentials are OK
            // This will cause exceptions to be thrown quite soon, but this way
            // the roles' OnStart() method returns correctly, allowing the web role
            // to display a warning to the user (the worker is recycled indefinitely
            // as Run() throws almost immediately)

            if (settings != null && !string.IsNullOrEmpty(settings.DataConnectionString))
            {
                builder.RegisterInstance(settings);
            }
        }
    }
}
