﻿using System.Threading;
using System.Threading.Tasks;
using Lokad.Cloud.Management;

namespace Lokad.Cloud.Mock
{
    public class NullProvisioning : IProvisioningProvider
    {
        public bool IsAvailable
        {
            get { return false; }
        }

        public Task SetWorkerInstanceCount(int count, CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() => { });
        }

        public Task<int> GetWorkerInstanceCount(CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() => 1);
        }
    }
}
