using System.Threading;
using System.Xml.Linq;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.Runtime;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.EntryPoint
{
    public class ApplicationEntryPoint : IApplicationEntryPoint
    {
        public void Run(XElement settings, IDeploymentReader deploymentReader, IApplicationEnvironment environment, CancellationToken cancellationToken)
        {
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

            var runtime = new Runtime(runtimeProviders, environment, cloudSettings, Observers.CreateRuntimeObserver(log));
            runtime.Execute();
        }

        public void OnSettingsChanged(XElement settings)
        {
        }
    }
}
