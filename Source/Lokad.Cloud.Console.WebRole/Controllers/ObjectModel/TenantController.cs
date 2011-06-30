﻿#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Linq;
using System.Web.Mvc;
using Lokad.Cloud.Console.WebRole.Framework.Discovery;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.Console.WebRole.Controllers.ObjectModel
{
    public abstract class TenantController : ApplicationController
    {
        protected LokadCloudHostedService HostedService { get; private set; }
        protected CloudStorageProviders Storage { get; private set; }
        protected IBlobStorageProvider Blobs { get; private set; }
        protected IQueueStorageProvider Queues { get; private set; }
        protected ITableStorageProvider Tables { get; private set; }

        protected TenantController(AzureDiscoveryInfo discoveryInfo) : base(discoveryInfo)
        {
        }

        protected void InitializeDeploymentTenant(string hostedServiceName)
        {
            CurrentHostedService = hostedServiceName;

            var services = DiscoveryInfo.LokadCloudDeployments;
            HostedService = services.Single(d => d.ServiceName == hostedServiceName);

            Storage = CloudStorage.ForAzureAccount(HostedService.StorageAccount).BuildStorageProviders();
            Blobs = Storage.NeutralBlobStorage;
            Queues = Storage.NeutralQueueStorage;
            Tables = Storage.NeutralTableStorage;
        }

        public abstract ActionResult ByHostedService(string hostedServiceName);

        public virtual ActionResult Index()
        {
            return RedirectToAction("ByHostedService", new { hostedServiceName = DiscoveryInfo.LokadCloudDeployments.First().ServiceName });
        }
    }
}