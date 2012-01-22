#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.Storage;

namespace Lokad.Cloud.Autofac.Diagnostics
{
    /// <summary>
    /// Storage for logging that do not log themselves (breaking potential cycles)
    /// </summary>
    internal class NeutralLogStorage
    {
        public IBlobStorageProvider BlobStorage { get; set; }
    }
}
