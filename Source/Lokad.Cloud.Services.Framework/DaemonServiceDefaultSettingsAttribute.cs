#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;

namespace Lokad.Cloud.Services.Framework
{
    /// <summary>
    /// Default settings for daemon services. The actual settings can be overriden
    /// at runtime by the service administrator, these settings therefore only apply
    /// when the service is seen the first time or has not been administered yet.
    /// </summary>
    /// <seealso cref="DaemonService"/>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class DaemonServiceDefaultSettingsAttribute : Attribute
    {
        /// <summary>
        /// Indicates whether the service is started automatically once deployed.
        /// </summary>
        public bool Disabled { get; set; }

        /// <summary>
        /// User-friendly name of the service.
        /// </summary>
        /// <remarks>
        /// If this value is <c>null</c> or empty, a default service name is chosen based on the class type.
        /// </remarks>
        public string Title { get; set; }
    }
}