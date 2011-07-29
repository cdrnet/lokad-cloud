#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Cloud.Services.Framework.Commands;
using Lokad.Cloud.Services.Framework.Instrumentation;
using Lokad.Cloud.Services.Runtime.Internal;

namespace Lokad.Cloud.Services.Runtime.WorkingSet
{
    /// <summary>
    /// Represents the full current runtime working set.
    /// While the runtime has a long lifetime, it can
    /// unload and replace its working set on demand,
    /// e.g. when a new app is deployed.
    /// </summary>
    /// <remarks>
    /// Primary responsibility of this class: Manage
    /// runtime cells and rearrange them on demand,
    /// e.g. when cell affinities change.
    /// </remarks>
    internal sealed class RuntimeWorkingSet
    {
        private byte[] _packageAssemblies;
        private byte[] _packageConfig;
        private CancellationToken _cancellationToken;
        private Dictionary<string, CellProcess> _cells;
        private RuntimeHandle _runtimeHandle;

        private RuntimeWorkingSet() {}

        /// <summary>
        /// Create a new runtime working set and start all its cell processes.
        /// </summary>
        internal static RuntimeWorkingSet StartNew(byte[] packageAssemblies, byte[] packageConfig, IEnumerable<CellArrangement> arrangements, ICloudRuntimeObserver observer, Action<ICloudCommand> sendCommand, CancellationToken cancellationToken)
        {
            var runtimeHandle = new RuntimeHandle(sendCommand, observer);

            return new RuntimeWorkingSet
                {
                    _packageAssemblies = packageAssemblies,
                    _packageConfig = packageConfig,
                    _cancellationToken = cancellationToken,
                    _runtimeHandle = runtimeHandle,
                    _cells = arrangements
                        .Select(a => CellProcess.Run(packageAssemblies, packageConfig, a.ServicesSettings, runtimeHandle, new CellHandle(a.CellName), cancellationToken))
                        .ToDictionary(c => c.CellName)
                };
        }

        /// <summary>
        /// Create an empty
        /// </summary>
        internal static RuntimeWorkingSet Empty
        {
            get { return new RuntimeWorkingSet(); }
        }

        /// <summary>
        /// Cancel all cells of the working set and wait until they shut down.
        /// </summary>
        internal void ShutdownWait()
        {
            // TODO: consider timeout

            if (_cells == null || _cells.Count == 0)
            {
                return;
            }

            // TODO: consider cancelling the common cancellation token directly instead.
            foreach (var cell in _cells)
            {
                cell.Value.Cancel();
            }
            Task.WaitAll(_cells.Select(c => c.Value.Task).ToArray());
        }

        /// <summary>
        /// Reconfigure the app package
        /// </summary>
        internal void Reconfigure(byte[] newPackageConfig)
        {
            if (_cells == null)
            {
                return;
            }

            _packageConfig = newPackageConfig;
            foreach (var cell in _cells)
            {
                cell.Value.Reconfigure(newPackageConfig);
            }
        }

        /// <summary>
        /// Rearranges the working set cells to fit the new provided arrangement.
        /// This involves shutting down cells no longer defined,
        /// starting up new cells that have not been defined before,
        /// and updating the service settings of the remaining cells.
        /// </summary>
        /// <param name="newSettings"></param>
        internal void Rearrange(IEnumerable<CellArrangement> newSettings)
        {
            // TODO: consider timeout

            if (_cells == null)
            {
                return;
            }

            // 1. ANALYSE CHANGES

            var removedCells = new Dictionary<string, CellProcess>(_cells);
            var addedArrangements = new List<CellArrangement>();
            var remainingArrangements = new List<CellArrangement>();

            foreach(var settings in newSettings)
            {
                if (_cells.ContainsKey(settings.CellName))
                {
                    removedCells.Remove(settings.CellName);
                    remainingArrangements.Add(settings);
                }
                else
                {
                    addedArrangements.Add(settings);
                }
            }

            // 2. REMOVE

            foreach (var cell in removedCells)
            {
                _cells.Remove(cell.Key);
                cell.Value.Cancel();
            }
            Task.WaitAll(removedCells.Select(c => c.Value.Task).ToArray());

            // 3. CHANGE

            foreach (var arrangement in remainingArrangements)
            {
                _cells[arrangement.CellName].ApplySettings(arrangement.ServicesSettings);
            }

            // 4. ADD

            foreach (var arrangement in addedArrangements)
            {
                _cells.Add(
                    arrangement.CellName,
                    CellProcess.Run(
                        _packageAssemblies,
                        _packageConfig,
                        arrangement.ServicesSettings,
                        _runtimeHandle,
                        new CellHandle(arrangement.CellName),
                        _cancellationToken));
            }
        }
    }
}
