﻿#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Net;
using Lokad.Cloud.Storage;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Lokad.Cloud
{
    /// <summary>
    /// Cloud Environment Helper
    /// </summary>
    /// <remarks>
    /// Providing functionality of Azure <see cref="RoleEnvironment"/>,
    /// but more neutral and resilient to missing runtime.
    /// </remarks>
    public static class CloudEnvironment
    {
        static bool _runtimeAvailable;
        static string _partitionKey;

        static CloudEnvironment()
        {
            try
            {
                _runtimeAvailable = RoleEnvironment.IsAvailable;
            }
            catch (TypeInitializationException)
            {
                _runtimeAvailable = false;
            }
        }

        /// <summary>
        /// Indicates whether the instance is running in the Cloud environment.
        /// </summary>
        public static bool IsAvailable
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

        /// <summary>
        /// Cloud Worker Key
        /// </summary>
        public static string PartitionKey
        {
            get { return _partitionKey ?? (_partitionKey = Dns.GetHostName()); }
        }

        ///<summary>
        /// Retreives the configuration setting from the <see cref="RoleEnvironment"/>.
        ///</summary>
        ///<param name="configurationSettingName">Name of the configuration setting</param>
        ///<returns>configuration value, or an empty result, if the environment is not present, or the value is null or empty</returns>
        public static Maybe<string> GetConfigurationSetting(string configurationSettingName)
        {
            if (!IsAvailable)
            {
                return Maybe<string>.Empty;
            }

            try
            {
                var value = RoleEnvironment.GetConfigurationSettingValue(configurationSettingName);
                if (!String.IsNullOrEmpty(value))
                {
                    value = value.Trim();
                }
                if (String.IsNullOrEmpty(value))
                {
                    value = null;
                }
                return value;
            }
            catch (RoleEnvironmentException)
            {
                return Maybe<string>.Empty;
                // setting was removed from the csdef, skip
                // (logging is usually not available at that stage)
            }
        }
    }
}