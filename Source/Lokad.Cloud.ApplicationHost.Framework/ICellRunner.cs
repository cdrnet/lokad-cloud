#region Copyright (c) Lokad 2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Threading;
using System.Xml.Linq;

namespace Lokad.Cloud.AppHost.Framework
{
    public interface ICellRunner
    {
        void Run(XElement settings, IDeploymentReader deploymentReader, IApplicationEnvironment environment, CancellationToken cancellationToken);
        void ApplyChangedSettings(XElement settings);
    }
}
