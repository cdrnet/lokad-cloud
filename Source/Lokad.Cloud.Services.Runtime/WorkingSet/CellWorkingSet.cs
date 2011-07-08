#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Cloud.Services.Runtime.WorkingSet
{
    /// <summary>
    /// Runtime working set for a single cell.
    /// Manages the cell's process and TPL-task.
    /// Supports selectively cancelling only this cell process.
    /// </summary>
    internal sealed class CellWorkingSet
    {
        private CancellationTokenSource _cancellationTokenSource;
        private CellProcess _process;

        public CellServiceSettings Settings { get; private set; }
        public Task Task { get; private set; }

        private CellWorkingSet() {}

        /// <summary>
        /// Create a new runtime cell working set and start its process.
        /// </summary>
        public static CellWorkingSet StartNew(byte[] packageAssemblies, byte[] packageConfig, CellServiceSettings settings, CancellationToken cancellationToken)
        {
            var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var process = new CellProcess(packageAssemblies, packageConfig, settings);

            return new CellWorkingSet
                {
                    _cancellationTokenSource = cancellationTokenSource,
                    Settings = settings,
                    _process = process,
                    Task = process.Run(cancellationTokenSource.Token)
                };
        }

        /// <summary>
        /// Selectively cancel only a single cell
        /// </summary>
        public void Cancel()
        {
            _cancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Reconfigure the app package
        /// </summary>
        public void Reconfigure(byte[] newPackageConfig)
        {
            _process.Reconfigure(newPackageConfig);
        }

        /// <summary>
        /// Apply new service settings
        /// </summary>
        public void ApplySettings(CellServiceSettings newSettings)
        {
            _process.ApplySettings(newSettings);
        }
    }
}
