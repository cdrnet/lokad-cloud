#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Threading;
using System.Xml.Linq;
using Lokad.Cloud.AppHost.AssembyLoading;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.AppHost.Util;

namespace Lokad.Cloud.AppHost
{
    /// <summary>
    /// AppDomain Entry Point for the cell process (single use).
    /// </summary>
    internal sealed class CellProcessAppDomainEntryPoint : MarshalByRefObject
    {
        private readonly CancellationTokenSource _externalCancellationTokenSource = new CancellationTokenSource();
        private ICellRunner _cellRunner;

        /// <remarks>Never run a cell process entry point more than once per AppDomain.</remarks>
        public void Run(string cellDefinitionXml, IDeploymentReader deploymentReader, ApplicationEnvironment environment)
        {
            var cellDefinition = XElement.Parse(cellDefinitionXml);

            // Load Assemblies into AppDomain
            var assembliesBytes = deploymentReader.GetItem<byte[]>(cellDefinition.SettingsElementAttributeValue("Assemblies", "name"));
            var loader = new AssemblyLoader();
            loader.LoadAssembliesIntoAppDomain(assembliesBytes);

            // Create Cell Runner
            var runnerTypeName = cellDefinition.SettingsElementAttributeValue("Runner", "typeName");
            var runnerType = string.IsNullOrEmpty(runnerTypeName) ? Type.GetType("Lokad.Cloud.Services.Framework.Runner.CellRunner") : Type.GetType(runnerTypeName);
            _cellRunner = (ICellRunner)Activator.CreateInstance(runnerType);

            // Run
            _cellRunner.Run((cellDefinition.SettingsElement("Settings") ?? new XElement("Settings")), deploymentReader, environment, _externalCancellationTokenSource.Token);
        }

        public void Cancel()
        {
            _externalCancellationTokenSource.Cancel();
        }

        public void AppplyChangedSettings(string settingsXml)
        {
            _cellRunner.ApplyChangedSettings(XElement.Parse(settingsXml));
        }
    }
}