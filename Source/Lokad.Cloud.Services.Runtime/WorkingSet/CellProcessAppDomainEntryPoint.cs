using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Cloud.Services.Framework;
using Lokad.Cloud.Services.Management.Settings;
using Lokad.Cloud.Services.Runtime.Runner;

namespace Lokad.Cloud.Services.Runtime.WorkingSet
{
    /// <summary>
    /// AppDomain Entry Point for the cell process.
    /// </summary>
    internal sealed class CellProcessAppDomainEntryPoint : MarshalByRefObject
    {
        private readonly object _startStopLock = new object();
        private CancellationTokenSource _externalCancellationTokenSource;
        private TaskCompletionSource<object> _completionSource;

        public void Run(EntryPointParameters parameters)
        {
            lock (_startStopLock)
            {
                _externalCancellationTokenSource = new CancellationTokenSource();
                _completionSource = new TaskCompletionSource<object>(TaskCreationOptions.LongRunning);
            }

            try
            {
                while (!_externalCancellationTokenSource.Token.IsCancellationRequested)
                {
                    var runner = new ServiceRunner();

                    try
                    {
                        runner.Run(new List<ICloudService>(), parameters.ServicesSettings, _externalCancellationTokenSource.Token);
                    }
                    catch (Exception)
                    {
                        
                        throw;
                    }
                }

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

        public void ApplySettings(CloudServicesSettings newServicesSettings)
        {

        }
    }

    [Serializable]
    internal sealed class EntryPointParameters
    {
        public byte[] PackageAssemblies { get; set; }
        public byte[] PackageConfig { get; set; }
        public string CellName { get; set; }
        public CloudServicesSettings ServicesSettings { get; set; }
    }
}