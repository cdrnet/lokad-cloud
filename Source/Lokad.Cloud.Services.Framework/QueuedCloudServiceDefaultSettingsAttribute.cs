#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;

namespace Lokad.Cloud.Services.Framework
{
    /// <summary>
    /// Default settings for queued cloud services. The actual settings can be overriden
    /// at runtime by the service administrator, these settings therefore only apply
    /// when the service is seen the first time or has not been administered yet.
    /// </summary>
    /// <seealso cref="QueuedCloudService{TMessage}"/>
    /// <seealso cref="UntypedQueuedCloudService"/>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class QueuedCloudServiceDefaultSettingsAttribute : Attribute
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

        /// <summary>
        /// Name of the queue attached to the queued service.
        /// </summary>
        /// <remarks>
        /// If this value is <c>null</c> or empty, a default service name is chosen based on the type <c>TMessage</c>.
        /// </remarks>
        public string QueueName { get; set; }

        /// <summary>
        /// Maximum number of times a message is tried to process before it is considered as
        /// being poisonous, removed from the queue and persisted to the 'failing-messages' store.
        /// </summary>
        /// <remarks>
        /// If the property is left at zero, then the default value of 5 is applied.
        /// </remarks>
        public int MaxProcessingTrials { get; set; }

        /// <summary>
        /// Time when the message will automatically reappear in the message queue
        /// after dequeueing if it has not been confirmed/deleted in the meantime.
        /// </summary>
        public double InvisibilityTimeoutSeconds { get; set; }

        /// <summary>
        /// Hint for the scheduler for how long (at max) the service should keep processing
        /// messages until switching the context to another service.
        /// </summary>
        public double ContinueForSeconds { get; set; }

        /// <summary>
        /// Execution timeout in seconds for the <c>OnQueueMessage</c> method of the service.
        /// It is <c>strongly</c> recommended to keep the timeout below 2 hours, or 7200 seconds.
        /// </summary>
        public double ProcessingTimeoutSeconds { get; set; }
    }
}