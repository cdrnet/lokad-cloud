using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Cloud.Services.Framework.Provisioning
{
    public class NullProvisioningProvider : IProvisioningProvider
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
