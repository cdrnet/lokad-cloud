#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;
using System.Security;
using System.Threading;
using System.Xml.Linq;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.Runtime;
using Lokad.Cloud.ServiceFabric.Runtime;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.EntryPoint
{
    public class ApplicationEntryPoint : IApplicationEntryPoint
    {
        public void Run(XElement settings, IDeploymentReader deploymentReader, IApplicationEnvironment environment, CancellationToken cancellationToken)
        {
            // NOTE: All assemblies of the app are already loaded at this point

            // Init
            var cloudSettings = new CloudConfigurationSettings
                {
                    DataConnectionString = settings.Element("DataConnectionString").Value,
                    SelfManagementCertificateThumbprint = settings.Element("CertificateThumbprint").Value,
                    SelfManagementSubscriptionId = settings.Element("SubscriptionId").Value
                };

            var log = new CloudLogWriter(CloudStorage.ForAzureConnectionString(settings.Element("DataConnectionString").Value).BuildBlobStorage());

            var runtimeFinalizer = new ServiceFabric.RuntimeFinalizer();
            var runtimeProviders = CloudStorage
                    .ForAzureConnectionString(settings.Element("DataConnectionString").Value)
                    .WithObserver(Observers.CreateStorageObserver(log))
                    .WithRuntimeFinalizer(runtimeFinalizer)
                    .BuildRuntimeProviders(log);

            try
            {
                // Run
                var runtime = new Runtime(runtimeProviders, environment, cloudSettings, Observers.CreateRuntimeObserver(log));
                runtime.Execute();

                log.DebugFormat("Runtime Host: Runtime has stopped cleanly on worker {0}.", environment.Host.WorkerName);
            }
            catch (TypeLoadException typeLoadException)
            {
                log.ErrorFormat(typeLoadException, "Runtime Host: Type {0} could not be loaded. The Runtime Host will be restarted.",
                    typeLoadException.TypeName);
            }
            catch (FileLoadException fileLoadException)
            {
                // Tentatively: referenced assembly is missing
                log.Fatal(fileLoadException, "Runtime Host: Could not load assembly probably due to a missing reference assembly. The Runtime Host will be restarted.");
            }
            catch (SecurityException securityException)
            {
                // Tentatively: assembly cannot be loaded due to security config
                log.FatalFormat(securityException, "Runtime Host: Could not load assembly {0} probably due to security configuration. The Runtime Host will be restarted.",
                    securityException.FailedAssemblyInfo);
            }
            catch (TriggerRestartException)
            {
                log.DebugFormat("Runtime Host: Triggered to stop execution on worker {0}. The Role Instance will be recycled and the Runtime Host restarted.",
                    environment.Host.WorkerName);

                environment.LoadCurrentHeadDeployment();
                throw;
            }
            catch (ThreadAbortException)
            {
                Thread.ResetAbort();
                log.DebugFormat("Runtime Host: execution was aborted on worker {0}. The Runtime is stopping.", environment.Host.WorkerName);
            }
            catch (Exception ex)
            {
                // Generic exception
                log.ErrorFormat(ex, "Runtime Host: An unhandled {0} exception occurred on worker {1}. The Runtime Host will be restarted.",
                    ex.GetType().Name, environment.Host.WorkerName);
            }
        }

        public void OnSettingsChanged(XElement settings)
        {
        }
    }
}
