#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using System.Threading;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.ServiceFabric
{
    /// <summary>Strongly-type queue service (inheritors are instantiated by
    /// reflection on the cloud).</summary>
    /// <typeparam name="T">Message type</typeparam>
    /// <remarks>
    /// <para>The implementation is not constrained by the 8kb limit for <c>T</c> instances.
    /// If the instances are larger, the framework will wrap them into the cloud storage.</para>
    /// <para>Whenever possible, we suggest to design the service logic to be idempotent
    /// in order to make the service reliable and ultimately consistent.</para>
    /// <para>A empty constructor is needed for instantiation through reflection.</para>
    /// </remarks>
    public abstract class QueueService<T> : CloudService
        where T : class
    {
        readonly string _queueName;
        readonly string _serviceName;
        readonly TimeSpan _visibilityTimeout;
        readonly TimeSpan _resilientAfter;
        readonly bool _resilientDequeue;
        readonly int _maxProcessingTrials;

        /// <summary>Name of the queue associated to the service.</summary>
        public override string Name
        {
            get { return _serviceName; }
        }

        /// <summary>Default constructor</summary>
        protected QueueService()
        {
            var settings = GetType().GetCustomAttributes(typeof(QueueServiceSettingsAttribute), true)
                                    .FirstOrDefault() as QueueServiceSettingsAttribute;

            // default settings
            _maxProcessingTrials = 5;

            if (null != settings) // settings are provided through custom attribute
            {
                _queueName = settings.QueueName ?? TypeMapper.GetStorageName(typeof (T));
                _serviceName = settings.ServiceName ?? GetType().FullName;

                if (settings.MaxProcessingTrials > 0)
                {
                    _maxProcessingTrials = settings.MaxProcessingTrials;
                }

                if (settings.ResilientDequeueAfterSeconds > 0)
                {
                    _resilientDequeue = true;
                    _resilientAfter = TimeSpan.FromSeconds(settings.ResilientDequeueAfterSeconds);
                }
                else
                {
                    _resilientAfter = TimeSpan.Zero;
                }
            }
            else
            {
                _queueName = TypeMapper.GetStorageName(typeof (T));
                _serviceName = GetType().FullName;
                _resilientAfter = TimeSpan.Zero;
            }

            // 1.25 * execution timeout, but limited to 2h max
            _visibilityTimeout = TimeSpan.FromSeconds(Math.Max(1, Math.Min(7200, (1.25*ExecutionTimeout.TotalSeconds))));
        }

        /// <summary>Do not try to override this method, use <see cref="Start"/> instead.</summary>
        protected sealed override ServiceExecutionFeedback StartImpl()
        {
            if (_resilientDequeue)
            {
                using(var message = QueueStorage.GetResilient<T>(_queueName, _resilientAfter, _maxProcessingTrials))
                {
                    if (message == null)
                    {
                        return ServiceExecutionFeedback.Skipped;
                    }

                    ProcessMessage(message.Message);
                    return ServiceExecutionFeedback.WorkAvailable;
                }
            }

            var messages = QueueStorage.Get<T>(_queueName, 1, _visibilityTimeout, _maxProcessingTrials).ToList();
            if (messages.Count == 0)
            {
                return ServiceExecutionFeedback.Skipped;
            }

            ProcessMessage(messages[0]);
            return ServiceExecutionFeedback.WorkAvailable;
        }

        private void ProcessMessage(T message)
        {
            try
            {
                Start(message);
            }
            catch (ThreadAbortException)
            {
                // no effect if the message has already been deleted, abandoned or resumed
                ResumeLater(message);
                throw;
            }
            catch (Exception)
            {
                // no effect if the message has already been deleted, abandoned or resumed
                Abandon(message);
                throw;
            }

            // no effect if the message has already been deleted, abandoned or resumed
            Delete(message);
        }

        /// <summary>Method called first by the <c>Lokad.Cloud</c> framework when a message is
        /// available for processing. The message is automatically deleted from the queue
        /// if the method returns (no deletion if an exception is thrown).</summary>
        protected abstract void Start(T message);

        /// <summary>
        /// Delete message retrieved through <see cref="Start"/>.
        /// </summary>
        public void Delete(T message)
        {
            QueueStorage.Delete(message);
        }

        /// <summary>
        /// Abandon a messages retrieved through <see cref="Start"/>
        /// and put it visibly back on the queue.
        /// </summary>
        public void Abandon(T message)
        {
            QueueStorage.Abandon(message);
        }

        /// <summary>
        /// Resume a message retrieved through <see cref="Start"/>
        /// later and put it visibly back on the queue,
        /// without decreasing the poison detection dequeue count.
        /// </summary>
        public void ResumeLater(T message)
        {
            QueueStorage.ResumeLater(message);
        }
    }
}
