﻿#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Lokad.Cloud.Services.Framework.Runner
{
    internal class ScheduledCloudServiceRunner : CommonServiceRunner<ScheduledCloudService>
    {
        public ScheduledCloudServiceRunner(List<ServiceWithSettings<ScheduledCloudService>> services)
            : base(services)
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
