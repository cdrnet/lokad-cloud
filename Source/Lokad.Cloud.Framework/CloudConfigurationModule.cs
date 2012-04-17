#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Autofac;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud
{
    /// <summary>
    /// IoC configuration module for Azure storage and management credentials.
    /// Recommended to be loaded either manually or in the appconfig.
    /// </summary>
    /// <remarks>
    /// When only using the storage (O/C mapping) toolkit standalone it is easier
    /// to let the <see cref="CloudStorage"/> factory create the storage providers on demand.
    /// </remarks>
    /// <seealso cref="CloudModule"/>
    /// <seealso cref="CloudStorage"/>
    public sealed class CloudConfigurationModule : Module
    {
        /// <summary>Azure storage connection string.</summary>
        public string DataConnectionString { get; set; }

        /// <summary>Azure subscription Id (optional).</summary>
        public string SelfManagementSubscriptionId { get; set; }

        /// <summary>Azure management certificate thumbprint (optional).</summary>
        public string SelfManagementCertificateThumbprint { get; set; }

        public CloudConfigurationModule()
        {
        }

        public CloudConfigurationModule(ICloudConfigurationSettings settings)
        {
            DataConnectionString = settings.DataConnectionString;
            SelfManagementSubscriptionId = settings.SelfManagementSubscriptionId;
            SelfManagementCertificateThumbprint = settings.SelfManagementCertificateThumbprint;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(c => MergeWithEnvironment(c.Resolve<IEnvironment>())).As<ICloudConfigurationSettings>();
        }

        RoleConfigurationSettings MergeWithEnvironment(IEnvironment environment)
        {
            return new RoleConfigurationSettings
                {
                    DataConnectionString = DataConnectionString ?? environment.GetSettingValue("DataConnectionString"),
                    SelfManagementSubscriptionId = SelfManagementSubscriptionId ?? environment.GetSettingValue("SelfManagementSubscriptionId"),
                    SelfManagementCertificateThumbprint = SelfManagementCertificateThumbprint ?? environment.GetSettingValue("SelfManagementCertificateThumbprint")
                };
        }
    }
}
