#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.ServiceFabric;

namespace Lokad.Cloud.Services
{
    /// <summary>
    /// Checks for updated assemblies or configuration and restarts the runtime if needed.
    /// </summary>
    [ScheduledServiceSettings(
           AutoStart = true,
           TriggerInterval = 60, // 1 execution every 1 minute
           Description = "Checks for and applies assembly and configuration updates.",
           ProcessingTimeoutSeconds = 5 * 60, // timeout after 5 minutes
           SchedulePerWorker = true)]
    public class AssemblyConfigurationUpdateService : ScheduledService
    {
        protected override void StartOnSchedule()
        {
            Environment.LoadCurrentHeadDeployment();
        }
    }
}
