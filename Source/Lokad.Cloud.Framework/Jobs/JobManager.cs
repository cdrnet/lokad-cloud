using System;
using Lokad.Cloud.Instrumentation;
using Lokad.Cloud.Instrumentation.Events;

namespace Lokad.Cloud.Jobs
{
    public class JobManager
    {
        private readonly IRuntimeObserver _observer;

        public JobManager(IRuntimeObserver observer = null)
        {
            _observer = observer;
        }

        public Job CreateNew()
        {
            return new Job
                {
                    JobId = string.Format("j{0:yyyyMMddHHnnss}{1:N}", DateTime.UtcNow, Guid.NewGuid())
                };
        }

        public Job StartNew()
        {
            var job = CreateNew();
            Start(job);
            return job;
        }

        public void Start(Job job)
        {
            if (_observer != null)
            {
                _observer.Notify(new JobStartedEvent(job));
            }
        }

        public void Succeed(Job job)
        {
            if (_observer != null)
            {
                _observer.Notify(new JobSucceededEvent(job));
            }
        }

        public void Fail(Job job)
        {
            if (_observer != null)
            {
                _observer.Notify(new JobFailedEvent(job));
            }
        }
    }
}
