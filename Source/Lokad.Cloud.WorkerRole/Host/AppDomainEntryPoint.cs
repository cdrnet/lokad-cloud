#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.AppHost.Framework.Definition;
using Lokad.Cloud.ServiceFabric.Runtime;

namespace Lokad.Cloud.Host
{
    /// <summary>
    /// Host for a single runtime instance.
    /// </summary>
    internal class AppDomainEntryPoint : MarshalByRefObject, IDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly EventWaitHandle _stoppedWaitHandle = new ManualResetEvent(false);

        /// <summary>
        /// Run the hosted runtime, blocking the calling thread.
        /// </summary>
        /// <returns>True if the worker stopped as planned (e.g. due to updated assemblies)</returns>
        public bool Run(CellDefinition cellDefinition, IDeploymentReader deploymentReader, ApplicationEnvironment environment)
        {
            _stoppedWaitHandle.Reset();
            try
            {
                // Load Assemblies into AppDomain
                var assemblies = deploymentReader.GetAssembliesAndSymbols(cellDefinition.Assemblies).ToList();
                var loader = new AssemblyLoader();
                loader.LoadAssembliesIntoAppDomain(assemblies, environment);

                // Create the EntryPoint
                var entryPointType = Type.GetType(cellDefinition.EntryPointTypeName);
                if (entryPointType == null)
                {
                    throw new InvalidOperationException("Typ " + cellDefinition.EntryPointTypeName + " not found.");
                }

                var entryPoint = (IApplicationEntryPoint)Activator.CreateInstance(entryPointType);
                var settings = String.IsNullOrEmpty(cellDefinition.SettingsXml) ? new XElement("Settings") : XElement.Parse(cellDefinition.SettingsXml);

                // Run
                entryPoint.Run(settings, deploymentReader, environment, _cancellationTokenSource.Token);
            }
            catch (TriggerRestartException)
            {
                return true;
            }
            finally
            {
                _stoppedWaitHandle.Set();
            }

            return false;
        }

        /// <summary>
        /// Immediately stop the runtime host and wait until it has exited (or a timeout expired).
        /// </summary>
        public void Stop()
        {
            _cancellationTokenSource.Cancel();

            // note: we DO have to wait until the shut down has finished,
            // or the Azure Fabric will tear us apart early!
            _stoppedWaitHandle.WaitOne(TimeSpan.FromSeconds(25));
        }

        public void Dispose()
        {
            _stoppedWaitHandle.Close();
        }
    }
}
