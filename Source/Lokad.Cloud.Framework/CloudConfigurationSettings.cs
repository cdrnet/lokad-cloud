#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;

namespace Lokad.Cloud
{
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
    }
}