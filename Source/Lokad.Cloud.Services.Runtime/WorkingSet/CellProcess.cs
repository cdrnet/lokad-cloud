using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Cloud.Services.Runtime.WorkingSet
{
    /// <summary>
    /// Cell process host a service runner for a single cell,
    /// isolated in its own AppDomain and in a separate thread.
    /// The cell runner will be automatically restarted in exceptional
    /// circumstances.
    /// </summary>
    internal sealed class CellProcess
    {
        private static readonly TimeSpan FloodFrequencyThreshold = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan DelayWhenFlooding = TimeSpan.FromMinutes(5);

        private CellServiceSettings _settings;
        private readonly byte[] _packageAssemblies;
        private byte[] _packageConfig;

        private CellProcessAppDomainEntryPoint _entryPoint;

        public CellProcess(
            byte[] packageAssemblies,
            byte[] packageConfig,
            CellServiceSettings settings)
        {
            _packageAssemblies = packageAssemblies;
            _packageConfig = packageConfig;
            _settings = settings;
        }

        public Task Run(CancellationToken cancellationToken)
        {
            var completionSource = new TaskCompletionSource<object>(TaskCreationOptions.LongRunning);

            var domain = AppDomain.CreateDomain("CellAppDomain_" + _settings.CellName, null, AppDomain.CurrentDomain.SetupInformation);

            try
            {
                _entryPoint = (CellProcessAppDomainEntryPoint)domain.CreateInstanceAndUnwrap(
                    Assembly.GetExecutingAssembly().FullName,
                    typeof(CellProcessAppDomainEntryPoint).FullName);
            }
            catch (Exception exception)
            {
                AppDomain.Unload(domain);
                completionSource.TrySetException(exception);
                return completionSource.Task;
            }

            // Forward cancellation token to internal token source
            cancellationToken.Register(_entryPoint.Cancel);

            // Unload the app domain in the end
            completionSource.Task.ContinueWith(task => AppDomain.Unload(domain));

            var thread = new Thread(() =>
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            _entryPoint.Run(new EntryPointSettings
                                {
                                    PackageAssemblies = _packageAssemblies,
                                    PackageConfig = _packageConfig
                                });
                        }
                        catch (ThreadAbortException)
                        {
                            Thread.ResetAbort();
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                    }

                    completionSource.TrySetCanceled();
                });

            thread.Start();

            return completionSource.Task;
        }

        public void Reconfigure(byte[] newPackageConfig)
        {
            _packageConfig = newPackageConfig;
            var entryPoint = _entryPoint;
            if (entryPoint != null)
            {
                entryPoint.Reconfigure(newPackageConfig);
            }
        }

        public void ApplySettings(CellServiceSettings newSettings)
        {
            _settings = newSettings;
            var entryPoint = _entryPoint;
            if (entryPoint != null)
            {
                entryPoint.ApplySettings(newSettings);
            }
        }
    }
}
