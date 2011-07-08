using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Lokad.Cloud.Services.Framework;
using Lokad.Cloud.Services.Management.Settings;

namespace Lokad.Cloud.Services.Runtime.Runner
{
    internal class ScheduledCloudServiceRunner
    {
        public ScheduledCloudServiceRunner(IEnumerable<ServiceWithSettings<ScheduledCloudService, ScheduledCloudServiceSettings>> services)
        {
            // TODO: Implement

            if (services.Any())
            {
                throw new NotImplementedException();
            }
        }

        public bool RunSingle(CancellationToken cancellationToken)
        {
            // TODO: Implement

            return false;
        }
    }
}
