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
                new CommandSettings("tool1", "dotnet", tempDirectory.WithFile("tool1.dll")),
                new CommandSettings("tool2", "dotnet", tempDirectory.WithFile("tool2.dll"))
            };

            NuGetFramework targetFramework = NuGetFramework.Parse("netcoreapp2.1");
            string runtimeIdentifier = "any";
            PackageId packageId = new PackageId("my.toolBundle");
            NuGetVersion nuGetVersion = NuGetVersion.Parse("1.0.2");
            localToolsResolverCache.Save(packageId, nuGetVersion, targetFramework, runtimeIdentifier,
                listOfCommandSettings, nuGetGlobalPackagesFolder);

            IReadOnlyList<CommandSettings> loadedResolverCache =
                localToolsResolverCache.Load(packageId, nuGetVersion, targetFramework, runtimeIdentifier);

            loadedResolverCache.Should().Contain(c =>
                c.Name == "tool1" && c.Runner == "dotnet" &&
                c.Executable.ToString() == nuGetGlobalPackagesFolder.WithFile("too1.dll").ToString());

            loadedResolverCache.Should().Contain(c =>
                c.Name == "tool2" && c.Runner == "dotnet" &&
                c.Executable.ToString() == nuGetGlobalPackagesFolder.WithFile("too2.dll").ToString());
        }
    }

    internal class LocalToolsResolverCache
    {
        private readonly DirectoryPath _cacheDirectory;
        private readonly IFileSystem _fileSystem;
        private readonly int _version;

        public LocalToolsResolverCache(IFileSystem fileSystem, DirectoryPath cacheDirectory, int version)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _cacheDirectory = cacheDirectory;
            _version = version;
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
            
            
        }

        private void EnsureFileStorageExists()
        {
            _fileSystem.Directory.CreateDirectory(_cacheDirectory.WithSubDirectories(_version.ToString()).Value);
        }

        private SerializableSchema Convert(NuGetVersion version,
            NuGetFramework targetFramework,
            string runtimeIdentifier,
            IReadOnlyList<CommandSettings> listOfCommandSettings,
            DirectoryPath nuGetGlobalPackagesFolder)
        {
            return new SerializableSchema(
            {
                Version = version.ToNormalizedString(),
                TargetFramework = targetFramework.GetShortFolderName(),
                RuntimeIdentifier = runtimeIdentifier,
                SerializableCommandSettingsArray =
                    listOfCommandSettings.Select(s => new SerializableCommandSettings
                    {
                        Name = s.Name, Runner = s.Runner,
                        RelativeToNuGetGlobalPackagesFolderPathToDll =
                            Path.GetRelativePath(nuGetGlobalPackagesFolder.Value, s.Executable.Value)
                    }).ToArray()
            };
        }

        private class SerializableSchema
        {
            public string Version { get; set; }
            public string TargetFramework { get; set; }
            public string RuntimeIdentifier  { get; set; }
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
