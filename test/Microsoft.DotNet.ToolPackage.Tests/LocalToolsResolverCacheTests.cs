// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Tools.Test.Utilities;
using Microsoft.Extensions.DependencyModel.Tests;
using Microsoft.Extensions.EnvironmentAbstractions;
using Newtonsoft.Json;
using NuGet.Frameworks;
using NuGet.Versioning;
using Xunit;

namespace Microsoft.DotNet.ToolPackage.Tests
{
    public class LocalToolsResolverCacheTests : TestBase
    {
        [Fact]
        public void
            GivenListOfCommandSettingsAndTargetFrameworkAndRidAndCurrentNugetCacheLocationItCanWriteAndRetrieve()
        {
            IFileSystem fileSystem = new FileSystemMockBuilder().UseCurrentSystemTemporaryDirectory().Build();
            DirectoryPath tempDirectory =
                new DirectoryPath(fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath);
            DirectoryPath cacheDirectory = tempDirectory.WithSubDirectories("cacheDirectory");
            DirectoryPath nuGetGlobalPackagesFolder = tempDirectory.WithSubDirectories("nugetGlobalPackageLocation");
            fileSystem.Directory.CreateDirectory(cacheDirectory.Value);
            int version = 1;

            LocalToolsResolverCache localToolsResolverCache =
                new LocalToolsResolverCache(fileSystem, cacheDirectory, version);
            IReadOnlyList<CommandSettings> listOfCommandSettings = new[]
            {
                new CommandSettings("tool1", "dotnet", nuGetGlobalPackagesFolder.WithFile("tool1.dll")),
                new CommandSettings("tool2", "dotnet", nuGetGlobalPackagesFolder.WithFile("tool2.dll"))
            };

            NuGetFramework targetFramework = NuGetFramework.Parse("netcoreapp2.1");
            string runtimeIdentifier = "any";
            PackageId packageId = new PackageId("my.toolBundle");
            NuGetVersion nuGetVersion = NuGetVersion.Parse("1.0.2");
            localToolsResolverCache.Save(packageId, nuGetVersion, targetFramework, runtimeIdentifier,
                listOfCommandSettings, nuGetGlobalPackagesFolder);

            IReadOnlyList<CommandSettings> loadedResolverCache =
                localToolsResolverCache.Load(packageId, nuGetVersion, targetFramework, runtimeIdentifier,
                    nuGetGlobalPackagesFolder);

            loadedResolverCache.Should().Contain(c =>
                c.Name == "tool1" && c.Runner == "dotnet" &&
                c.Executable.ToString() == nuGetGlobalPackagesFolder.WithFile("tool1.dll").ToString());

            loadedResolverCache.Should().Contain(c =>
                c.Name == "tool2" && c.Runner == "dotnet" &&
                c.Executable.ToString() == nuGetGlobalPackagesFolder.WithFile("tool2.dll").ToString());
        }
    }

    // TODO WUL nochecin move to a different file
    internal class LocalToolsResolverCache
    {
        private readonly DirectoryPath _cacheDirectory;
        private readonly DirectoryPath _cacheVersionedDirectory;
        private readonly IFileSystem _fileSystem;
        private readonly int _version;

        public LocalToolsResolverCache(IFileSystem fileSystem, DirectoryPath cacheDirectory, int version)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _cacheDirectory = cacheDirectory;
            _version = version;
            _cacheVersionedDirectory = _cacheDirectory.WithSubDirectories(_version.ToString());
        }

        public void Save(
            PackageId packageId,
            NuGetVersion version,
            NuGetFramework targetFramework,
            string runtimeIdentifier,
            IReadOnlyList<CommandSettings> listOfCommandSettings,
            DirectoryPath nuGetGlobalPackagesFolder)
        {
            EnsureFileStorageExists();
            CacheRow serializableSchema = Convert(
                version,
                targetFramework,
                runtimeIdentifier,
                listOfCommandSettings,
                nuGetGlobalPackagesFolder);

            string json = JsonConvert.SerializeObject(new[] {serializableSchema});
            _fileSystem.File.WriteAllText(GetCacheFile(packageId), json);
        }

        private string GetCacheFile(PackageId packageId)
        {
            return _cacheVersionedDirectory.WithFile(packageId.ToString()).Value;
        }


        private void EnsureFileStorageExists()
        {
            _fileSystem.Directory.CreateDirectory(_cacheVersionedDirectory.Value);
        }

        private CacheRow Convert(NuGetVersion version,
            NuGetFramework targetFramework,
            string runtimeIdentifier,
            IReadOnlyList<CommandSettings> listOfCommandSettings,
            DirectoryPath nuGetGlobalPackagesFolder)
        {
            return new CacheRow
            {
                Version = version.ToNormalizedString(),
                TargetFramework = targetFramework.GetShortFolderName(),
                RuntimeIdentifier = runtimeIdentifier,
                SerializableCommandSettingsArray =
                    listOfCommandSettings.Select(s => new SerializableCommandSettings
                    {
                        Name = s.Name,
                        Runner = s.Runner,
                        RelativeToNuGetGlobalPackagesFolderPathToDll =
                            Path.GetRelativePath(nuGetGlobalPackagesFolder.Value, s.Executable.Value)
                    }).ToArray()
            };
        }

        public IReadOnlyList<CommandSettings> Load(
            PackageId packageId,
            NuGetVersion version,
            NuGetFramework targetFramework,
            string runtimeIdentifier,
            DirectoryPath nuGetGlobalPackagesFolder)
        {
            string packageCacheFile = GetCacheFile(packageId);
            if (_fileSystem.File.Exists(packageCacheFile))
            {
                CacheRow[] cacheTable =
                    JsonConvert.DeserializeObject<CacheRow[]>(_fileSystem.File.ReadAllText(packageCacheFile));

                SerializableCommandSettings[] matchingCommandSettingsArray = cacheTable
                    .SingleOrDefault(row => row.Version == version.ToNormalizedString() &&
                                            row.TargetFramework == targetFramework.GetShortFolderName() &&
                                            row.RuntimeIdentifier == runtimeIdentifier)
                    ?.SerializableCommandSettingsArray;

                if (matchingCommandSettingsArray != null)
                {
                    return matchingCommandSettingsArray.Select(
                        serializableCommandSettings =>
                            new CommandSettings(
                                serializableCommandSettings.Name,
                                serializableCommandSettings.Runner,
                                nuGetGlobalPackagesFolder.WithFile(serializableCommandSettings
                                    .RelativeToNuGetGlobalPackagesFolderPathToDll))
                    ).ToArray();
                }
            }

            return Array.Empty<CommandSettings>();
        }

        private class CacheRow
        {
            public string Version { get; set; }
            public string TargetFramework { get; set; }
            public string RuntimeIdentifier { get; set; }
            public SerializableCommandSettings[] SerializableCommandSettingsArray { get; set; }
        }

        private class SerializableCommandSettings
        {
            public string Name { get; set; }
            public string Runner { get; set; }
            public string RelativeToNuGetGlobalPackagesFolderPathToDll { get; set; }
        }
    }
}


//given IReadOnlyList<CommandSettings> target frameowork and rid and current nuget cache location. it can write it down 
//
//give it package id, package version, command name, target frameowork and rid, it can give back commandsettings 
//
//give it package id, package version range, command name, target frameowork and rid, it can give back 
//
//different version of resolver cache is different. cannot resolve each other's cache 

// only save diff

// handle read and write concurrency
