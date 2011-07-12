#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Threading;
using System.Threading.Tasks;
using Lokad.Cloud.Services.Management.Settings;

namespace Lokad.Cloud.Services.Runtime.WorkingSet
{
    internal sealed class CellWorkingSet
    {
        public string CellName { get; set; }
        public CloudServicesSettings ServicesSettings { get; set; }
        public CellProcess Process { get; set; }
        public Task Task { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; }
    }
}
