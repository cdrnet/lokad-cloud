﻿#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.IO;
using System.Web;
using System.Web.Mvc;
using Lokad.Cloud.Console.WebRole.Behavior;
using Lokad.Cloud.Console.WebRole.Controllers.ObjectModel;
using Lokad.Cloud.Console.WebRole.Framework.Discovery;
using Lokad.Cloud.Console.WebRole.Models.Assemblies;
using Lokad.Cloud.Services.Management.Application;

namespace Lokad.Cloud.Console.WebRole.Controllers
{
    [RequireAuthorization, RequireDiscovery]
    public sealed class AssembliesController : TenantController
    {
        public AssembliesController(AzureDiscoveryInfo discoveryInfo)
            : base(discoveryInfo)
        {
        }

        [HttpGet]
        public override ActionResult ByHostedService(string hostedServiceName)
        {
            InitializeDeploymentTenant(hostedServiceName);
            var cloudAssemblies = new Services.Management.CloudAssemblies(Storage);
            var appDefinition = cloudAssemblies.GetApplicationDefinition();

            return View(new AssembliesModel
                {
                    ApplicationAssemblies = appDefinition.HasValue ? 
                        appDefinition.Value.Assemblies : new CloudApplicationAssemblyInfo[0]
                });
        }

        [HttpPost]
        public ActionResult UploadPackage(string hostedServiceName, HttpPostedFileBase package)
        {
            InitializeDeploymentTenant(hostedServiceName);
            var cloudAssemblies = new Services.Management.CloudAssemblies(Storage);

            byte[] bytes;
            using (var reader = new BinaryReader(package.InputStream))
            {
                bytes = reader.ReadBytes(package.ContentLength);
            }

            switch ((Path.GetExtension(package.FileName) ?? string.Empty).ToLowerInvariant())
            {
                case ".dll":
                    cloudAssemblies.UploadApplicationSingleDll(bytes, package.FileName);
                    break;

                default:
                    cloudAssemblies.UploadApplicationZipContainer(bytes);
                    break;
            }

            return RedirectToAction("ByHostedService");
        }
    }
}
