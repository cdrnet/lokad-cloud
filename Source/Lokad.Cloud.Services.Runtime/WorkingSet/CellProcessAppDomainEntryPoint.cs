using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Cloud.Services.Runtime.WorkingSet
{
    /// <summary>
    /// AppDomain Entry Point for the cell process.
    /// </summary>
    internal sealed class CellProcessAppDomainEntryPoint : MarshalByRefObject
    {
        private readonly object _startStopLock = new object();
        private CancellationTokenSource _externalCancellationTokenSource;
        private CancellationTokenSource _runnerCancellationTokenSource;
        private TaskCompletionSource<object> _completionSource;

        public void Run(EntryPointSettings settings)
        {
            lock (_startStopLock)
            {
                _externalCancellationTokenSource = new CancellationTokenSource();
                _runnerCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_externalCancellationTokenSource.Token);
                _completionSource = new TaskCompletionSource<object>(TaskCreationOptions.LongRunning);
            }

            try
            {
                // load assemblies and config

                // load and run cell runner (sync)

                _completionSource.TrySetResult(null);
            }
            catch (ThreadAbortException)
            {
                _completionSource.TrySetCanceled();
                Thread.ResetAbort();
            }
            catch (Exception exception)
            {
                _completionSource.TrySetException(exception);
            }
        }

        public void Cancel()
        {
            _externalCancellationTokenSource.Cancel();
        }

        public void ShutdownWait()
        {
            CancellationTokenSource cancellationToken;
            TaskCompletionSource<object> completionSource;

            lock (_startStopLock)
            {
                cancellationToken = _externalCancellationTokenSource;
                completionSource = _completionSource;
            }

            // TODO: Consider a timeout
            cancellationToken.Cancel();
            completionSource.Task.Wait();
        }

        public void Reconfigure(byte[] newPackageConfig)
        {

        }

        public void ApplySettings(CellServiceSettings newSettings)
        {

        }
    }

    [Serializable]
    internal sealed class EntryPointSettings
    {
        public byte[] PackageAssemblies { get; set; }
        public byte[] PackageConfig { get; set; }
    }
}