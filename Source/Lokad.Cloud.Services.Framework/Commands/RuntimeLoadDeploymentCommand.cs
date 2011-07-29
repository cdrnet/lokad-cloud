#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;

namespace Lokad.Cloud.Services.Framework.Commands
{
    [Serializable]
    public sealed class RuntimeLoadDeploymentCommand : ICloudCommand
    {
        public string DeploymentName { get; private set; }

        public RuntimeLoadDeploymentCommand(string deploymentName)
        {
            DeploymentName = deploymentName;
        }
    }
}
