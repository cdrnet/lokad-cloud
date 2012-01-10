#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Lokad.Cloud
{
    /// <summary>
    /// Settings used among others by the <see cref="Lokad.Cloud.Storage.Azure.StorageModule" />.
    /// </summary>
    [Serializable]
    public class CloudConfigurationSettings
    {
        /// <summary>
        /// Gets the data connection string.
        /// </summary>
        /// <value>The data connection string.</value>
        public string DataConnectionString { get; set; }

        /// <summary>
        /// Gets the Azure subscription Id to be used for self management (optional, can be null).
        /// </summary>
        public string SelfManagementSubscriptionId { get; set; }

        /// <summary>
        /// Gets the Azure certificate thumbpring to be used for self management (optional, can be null).
        /// </summary>
        public string SelfManagementCertificateThumbprint { get; set; }

        public static CloudConfigurationSettings LoadFromRoleEnvironment()
        {
            var settings = new CloudConfigurationSettings();
            
            try
            {
                if (!RoleEnvironment.IsAvailable)
                {
                    return settings;
                }
            }
            catch (TypeInitializationException)
            {
                return settings;
            }

            ApplySettingFromRole("DataConnectionString", v => settings.DataConnectionString = v);
            ApplySettingFromRole("SelfManagementSubscriptionId", v => settings.SelfManagementSubscriptionId = v);
            ApplySettingFromRole("SelfManagementCertificateThumbprint", v => settings.SelfManagementCertificateThumbprint = v);
            return settings;
        }

        static void ApplySettingFromRole(string setting, Action<string> setter)
        {
            try
            {
                var value = RoleEnvironment.GetConfigurationSettingValue(setting);
                if (!String.IsNullOrEmpty(value))
                {
                    value = value.Trim();
                }
                if (!String.IsNullOrEmpty(value))
                {
                    setter(value);
                }
            }
            catch (RoleEnvironmentException)
            {
                // setting was removed from the csdef, skip
                // (logging is usually not available at that stage)
            }
        }
    }
}