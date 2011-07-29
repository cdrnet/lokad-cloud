#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Cloud.Services.Framework.Instrumentation;
using Lokad.Cloud.Services.Runtime.Legacy;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.Services.Runtime
{
    public sealed class HybridRuntime
    {
        private readonly Runtime _runtime;

        public HybridRuntime(CloudStorageProviders storage, ICloudRuntimeObserver observer = null)
        {
            _runtime = new Runtime(storage, observer);
        }

        public Task Run(CancellationToken cancellationToken)
        {
            var cancelNewTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            return Task.Factory.ContinueWhenAll(new[]
                {
                    _runtime.RunAsync(cancelNewTokenSource.Token),
                    RunLegacyRuntime(cancellationToken, cancelNewTokenSource)
                },
                tasks => { });
        }

        Task RunLegacyRuntime(CancellationToken cancellationToken, CancellationTokenSource cancelNewRuntime)
        {
            var host = new ServiceFabricHost();
            host.StartRuntime();

            var completionSource = new TaskCompletionSource<object>();
            cancellationToken.Register(host.ShutdownRuntime);

            // Classic thread because the legacy runtime was designed to run in the main thread exclusively
            var thread = new Thread(() =>
            {
                try
                {
                    host.Run();
                    completionSource.TrySetResult(null);
                }
                catch (ThreadAbortException)
                {
                    Thread.ResetAbort();
                    completionSource.TrySetCanceled();
                }
                catch (Exception exception)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        // assuming the exception was caused by the cancellation
                        completionSource.TrySetCanceled();
                    }
                    else
                    {
                        completionSource.TrySetException(exception);
                    }
                }
                finally
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        // cancel the other runtime, so we can recycle
                        // (expected behavior of the legacy runtime)

                        // TODO: this may throw
                        cancelNewRuntime.Cancel();
                    }
                }
            });

            thread.Name = "Lokad.Cloud Legacy Runtime";
            thread.Start();

            return completionSource.Task;
        }
    }
}
