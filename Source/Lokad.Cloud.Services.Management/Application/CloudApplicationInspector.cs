#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lokad.Cloud.Services.Framework;
using Lokad.Cloud.Storage;
using Mono.Cecil;

namespace Lokad.Cloud.Services.Management.Application
{
    /// <summary>
    /// Utility to inspect a cloud application package and its services.
    /// </summary>
    /// <remarks>
    /// Mono.Cecil is used instead of .NET Reflector:
    /// 1. So we don't need to use an AppDomain to be able to unload the assemblies afterwards.
    /// 2. So we can reflect without resolving and loading any dependent assemblies (AutoFac, Lookad.Cloud.Storage)
    /// </remarks>
    public class CloudApplicationInspector
    {
        // TODO (ruegg, 2011-07-04): Drop legacy service definitions

        private const string AssembliesContainerName = "lokad-cloud-services";
        private const string PackageBlobName = "package.assemblies.lokadcloud";
        private const string ApplicationDefinitionBlobName = "package.definition.lokadcloud";

        private readonly CloudStorageProviders _storage;

        public CloudApplicationInspector(CloudStorageProviders storage)
        {
            _storage = storage;
        }

        public Maybe<CloudApplicationDefinition> Inspect()
        {
            var definitionBlob = _storage.NeutralBlobStorage.GetBlob<CloudApplicationDefinition>(AssembliesContainerName, ApplicationDefinitionBlobName);
            Maybe<byte[]> packageBlob;
            string packageETag;

            if (definitionBlob.HasValue)
            {
                packageBlob = _storage.NeutralBlobStorage.GetBlobIfModified<byte[]>(AssembliesContainerName, PackageBlobName, definitionBlob.Value.PackageETag, out packageETag);
                if (!packageBlob.HasValue || definitionBlob.Value.PackageETag == packageETag)
                {
                    return definitionBlob.Value;
                }
            }
            else
            {
                packageBlob = _storage.NeutralBlobStorage.GetBlob<byte[]>(AssembliesContainerName, PackageBlobName, out packageETag);
            }

            if (!packageBlob.HasValue)
            {
                return Maybe<CloudApplicationDefinition>.Empty;
            }

            var definition = Analyze(packageBlob.Value, packageETag);
            _storage.NeutralBlobStorage.PutBlob(AssembliesContainerName, ApplicationDefinitionBlobName, definition);
            return definition;
        }

        public static CloudApplicationDefinition Analyze(byte[] packageData, string etag)
        {
            var reader = new CloudApplicationPackageReader();
            var package = reader.ReadPackage(packageData, true);

            var queuedCloudServiceTypeDefinitions = new List<TypeDefinition>();
            var scheduledCloudServiceTypeDefinitions = new List<TypeDefinition>();
            var scheduledWorkerServiceTypeDefinitions = new List<TypeDefinition>();
            var daemonServiceTypeDefinitions = new List<TypeDefinition>();

            var typeDefinitionMaps = new Dictionary<string, TypeDefinition>();
            var serviceBaseTypes = new Dictionary<string, List<TypeDefinition>>
                {
                    { typeof(QueuedCloudService<>).FullName, queuedCloudServiceTypeDefinitions },
                    { typeof(ScheduledCloudService).FullName, scheduledCloudServiceTypeDefinitions },
                    { typeof(ScheduledWorkerService).FullName, scheduledWorkerServiceTypeDefinitions },
                    { typeof(DaemonService).FullName, daemonServiceTypeDefinitions }
                };

            // Instead of resolving, we reflect iteratively with multiple passes.
            // This way we can avoid loading referenced assemblies like AutoFac
            // and Lokad.Cloud.Storage which may have mismatching versions
            // (e.g. Autofac 2 is completely incompatible to Autofac 1)

            var assebliesBytes = package.Assemblies.Select(package.GetAssembly).ToList();
            assebliesBytes.Add(File.ReadAllBytes(typeof(ICloudService).Assembly.Location));
            // NOTE: Add here any assemblies from Lokad.Cloud that contain predefined cloud services

            bool newTypesFoundThisRound;
            do
            {
                newTypesFoundThisRound = false;
                foreach (var assemblyBytes in assebliesBytes)
                {
                    using (var stream = new MemoryStream(assemblyBytes))
                    {
                        var definition = AssemblyDefinition.ReadAssembly(stream);
                        foreach (var typeDef in definition.MainModule.Types)
                        {
                            if (typeDef.BaseType == null || typeDef.BaseType.FullName == "System.Object" || serviceBaseTypes.ContainsKey(typeDef.FullName))
                            {
                                continue;
                            }

                            var baseTypeName = typeDef.BaseType.IsGenericInstance
                                ? typeDef.BaseType.Namespace + "." + typeDef.BaseType.Name
                                : typeDef.BaseType.FullName;

                            List<TypeDefinition> matchingServiceTypes;
                            if (!serviceBaseTypes.TryGetValue(baseTypeName, out matchingServiceTypes))
                            {
                                continue;
                            }

                            typeDefinitionMaps.Add(typeDef.FullName, typeDef);
                            serviceBaseTypes.Add(typeDef.FullName, matchingServiceTypes);
                            newTypesFoundThisRound = true;

                            if (!typeDef.IsAbstract && !typeDef.HasGenericParameters)
                            {
                                matchingServiceTypes.Add(typeDef);
                            }
                        }
                    }
                }
            }
            while (newTypesFoundThisRound);

            return new CloudApplicationDefinition
                {
                    PackageETag = etag,
                    Timestamp = DateTimeOffset.UtcNow,
                    Assemblies = package.Assemblies.ToArray(),

                    DaemonServices = daemonServiceTypeDefinitions.Select(td => new DaemonServiceDefinition { TypeName = td.FullName }).ToArray(),
                    QueuedCloudServices = queuedCloudServiceTypeDefinitions.Select(td =>
                        {
                            var messageType = GetQueuedCloudServiceMessageType(td, typeDefinitionMaps);
                            return new QueuedCloudServiceDefinition
                                {
                                    TypeName = td.FullName,
                                    MessageTypeName = messageType.FullName,
                                    QueueName = GetAttributeProperty(td, typeof(QueuedCloudServiceDefaultSettingsAttribute).FullName, "QueueName", () => messageType.FullName.ToLowerInvariant().Replace(".", "-"))
                                };
                        }).ToArray(),
                    ScheduledCloudServices = scheduledCloudServiceTypeDefinitions.Select(td => new ScheduledCloudServiceDefinition { TypeName = td.FullName }).ToArray(),
                    ScheduledWorkerServices = scheduledWorkerServiceTypeDefinitions.Select(td => new ScheduledWorkerServiceDefinition { TypeName = td.FullName }).ToArray()
                };
        }

        private static T GetAttributeProperty<T>(TypeDefinition type, string attributeName, string propertyName, Func<T> defaultValue)
        {
            var attribute = type.CustomAttributes.FirstOrDefault(ca => ca.AttributeType.FullName == attributeName);
            if (attribute == null)
            {
                return defaultValue();
            }

            var property = attribute.Properties.FirstOrDefault(p => p.Name == propertyName);
            if (property.Name == null)
            {
                return defaultValue();
            }

            return (T)property.Argument.Value;
        }

        private static TypeReference GetQueuedCloudServiceMessageType(TypeDefinition typeDefinition, Dictionary<string, TypeDefinition> typeDefinitionMaps)
        {
            var baseRef = typeDefinition.BaseType;
            var baseRefName = baseRef.Namespace + "." + baseRef.Name;
            if (baseRefName == typeof(QueuedCloudService<>).FullName)
            {
                return ((GenericInstanceType)baseRef).GenericArguments[0];
            }

            var parentMessageType = GetQueuedCloudServiceMessageType(typeDefinitionMaps[baseRefName], typeDefinitionMaps);
            if (!parentMessageType.IsGenericParameter)
            {
                return parentMessageType;
            }

            return ((GenericInstanceType)baseRef).GenericArguments[0];
        }
    }
}
