#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

namespace Lokad.Cloud.Services.Management.Deployments
{
    public class DeploymentReference
    {
        public string Name { get; private set; }
        public string AssembliesName { get; private set; }
        public string ConfigName { get; private set; }
        public string SettingsName { get; private set; }

        public DeploymentReference(string name, string assembliesName, string configName, string settingsName)
        {
            Name = name;
            AssembliesName = assembliesName;
            ConfigName = configName;
            SettingsName = settingsName;
        }
    }
}
