#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.AppHost.Framework.Commands;
using Lokad.Cloud.AppHost.Framework.Events;
using Lokad.Cloud.AppHost.Util;

namespace Lokad.Cloud.AppHost
{
    public sealed class Host
    {
        private readonly IHostContext _hostContext;
        private readonly IHostObserver _observer;
        private readonly HostHandle _hostHandle;
        private readonly Dictionary<string, Cell> _cells;
        private readonly ConcurrentQueue<IHostCommand> _commandQueue;
        private readonly DeploymentHeadPollingAgent _deploymentPollingAgent;
        
        private string _currentDeploymentName;
        private XElement _currentDeploymentDefinition;

        public Host(IHostContext context, IHostObserver observer = null)
        {
            _hostContext = context;
            _observer = observer;
            _cells = new Dictionary<string, Cell>();
            _commandQueue = new ConcurrentQueue<IHostCommand>();
            _deploymentPollingAgent = new DeploymentHeadPollingAgent(context.DeploymentReader, _commandQueue.Enqueue);

            _hostHandle = new HostHandle(_commandQueue.Enqueue, observer);
        }

        public void RunSync(CancellationToken cancellationToken)
        {
            _observer.TryNotify(() => new HostStartedEvent());

            try
            {
                _currentDeploymentName = null;
                _currentDeploymentDefinition = new XElement("Deployment");
                while (!cancellationToken.IsCancellationRequested)
                {
                    // 1. apply all commands
                    IHostCommand command;
                    if (_commandQueue.TryDequeue(out command))
                    {
                        // dynamic dispatch, good enough for now
                        Do((dynamic)command, cancellationToken);
                        continue;
                    }

                    // 2. run agents
                    _deploymentPollingAgent.PollForChanges(_currentDeploymentName);

                    // 3. repeat, but throttled
                    cancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(30));
                }
            }
            finally
            {
                _observer.TryNotify(() => new HostStoppedEvent());
            }
        }

        public Task Run(CancellationToken cancellationToken)
        {
            var completionSource = new TaskCompletionSource<object>();

            var thread = new Thread(() =>
                {
                    try
                    {
                        RunSync(cancellationToken);
                        completionSource.TrySetResult(null);
                    }
                    catch (ThreadAbortException)
                    {
                        Thread.ResetAbort();
                        completionSource.TrySetCanceled();
                    }
                    catch (Exception exception)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            // assuming the exception was caused by the cancellation
                            completionSource.TrySetCanceled();
                        }
                        else
                        {
                            completionSource.TrySetException(exception);
                        }
                    }
                })
                {
                    Name = "Lokad.Cloud.ApplicationHost"
                };

            thread.Start();

            return completionSource.Task;
        }

        // Greatly simplified command handling for internal use only, can easily be refactored later if necessary
        // (pattern mainly used for simpler handling and queueing, not for cqs/cqrs-like ideas)

        void Do(LoadCurrentHeadDeploymentCommand command, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            _deploymentPollingAgent.PollForChanges(_currentDeploymentName);
        }

        void Do(LoadDeploymentCommand command, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var current = _currentDeploymentName;
            if (current != null && command.DeploymentName == current)
            {
                // already on requested deployment
                return;
            }

            var newDeploymentDefinition = _hostContext.DeploymentReader.GetItem<XElement>(command.DeploymentName);
            if (newDeploymentDefinition == null)
            {
                // TODO: NOTIFY/LOG invalid deployment
                return;
            }

            if (!XNode.DeepEquals(_currentDeploymentDefinition, newDeploymentDefinition))
            {
                ApplyChangedDeploymentDefinition(newDeploymentDefinition, command.DeploymentName, cancellationToken);
            }
        }

        void ApplyChangedDeploymentDefinition(XElement newDeploymentDefinition, string newDeploymentName, CancellationToken cancellationToken)
        {
            // 0. ANALYZE CELL LAYOUT CHANGES

            var old = _currentDeploymentDefinition
                .SettingsElements("Cells", "Cell")
                .ToDictionary(cellDefinition => cellDefinition.AttributeValue("name"));

            var removed = new Dictionary<string, Cell>(_cells);
            var added = new List<XElement>();
            var remaining = new List<XElement>();

            foreach (var newCellDefinition in newDeploymentDefinition.SettingsElements("Cells", "Cell"))
            {
                var cellName = newCellDefinition.AttributeValue("name");
                if (old.ContainsKey(cellName))
                {
                    removed.Remove(cellName);
                    remaining.Add(newCellDefinition);
                }
                else
                {
                    added.Add(newCellDefinition);
                }
            }

            // 1. UPDATE

            _currentDeploymentDefinition = newDeploymentDefinition;
            _currentDeploymentName = newDeploymentName;

            // 2. REMOVE CELLS NO LONGER PRESENT

            foreach (var cell in removed)
            {
                _cells.Remove(cell.Key);
                cell.Value.Cancel();
            }
            //Task.WaitAll(removed.Select(c => c.Value.Task).ToArray());

            // 3. UPDATE CELLS STILL PRESENT

            foreach (var newCellDefinition in remaining)
            {
                var cellName = newCellDefinition.AttributeValue("name");
                var oldCellDefinition = old[cellName];
                if (!XNode.DeepEquals(newCellDefinition, oldCellDefinition))
                {
                    _cells[cellName].ApplyChangedCellDefinition(newCellDefinition, newDeploymentName);
                }
            }

            // 4. ADD NEW CELLS

            foreach (var cellDefinition in added)
            {
                var cellName = cellDefinition.AttributeValue("name");
                _cells.Add(cellName, Cell.Run(_hostContext, _hostHandle, cellDefinition, newDeploymentName, cancellationToken));
            }
        }
    }
}
