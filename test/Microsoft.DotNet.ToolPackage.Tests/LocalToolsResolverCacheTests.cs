// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using FluentAssertions;
using Microsoft.DotNet.ToolPackage;
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
        public void GivenExecutableIdentifierItCanSaveAndLoad()
        {
            (DirectoryPath nuGetGlobalPackagesFolder, LocalToolsResolverCache localToolsResolverCache) = Setup();

            NuGetFramework targetFramework = NuGetFramework.Parse("netcoreapp2.1");
            string runtimeIdentifier = "any";
            PackageId packageId = new PackageId("my.toolBundle");
            NuGetVersion nuGetVersion = NuGetVersion.Parse("1.0.2");
            IReadOnlyList<CommandSettings> listOfCommandSettings = new[]
            {
                new CommandSettings("tool1", "dotnet", nuGetGlobalPackagesFolder.WithFile("tool1.dll")),
                new CommandSettings("tool2", "dotnet", nuGetGlobalPackagesFolder.WithFile("tool2.dll"))
            };

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

        [Fact]
        public void GivenExecutableIdentifierItCanSaveMultipleVersionAndLoad()
        {
            (DirectoryPath nuGetGlobalPackagesFolder, LocalToolsResolverCache localToolsResolverCache) = Setup();

            NuGetFramework targetFramework = NuGetFramework.Parse("netcoreapp2.1");
            string runtimeIdentifier = "any";
            PackageId packageId = new PackageId("my.toolBundle");
            NuGetVersion nuGetVersion = NuGetVersion.Parse("1.0.2");
            IReadOnlyList<CommandSettings> listOfCommandSettings = new[]
            {
                new CommandSettings("tool1", "dotnet", nuGetGlobalPackagesFolder.WithFile("tool1.dll")),
                new CommandSettings("tool2", "dotnet", nuGetGlobalPackagesFolder.WithFile("tool2.dll"))
            };
            
            NuGetVersion newerNuGetVersion = NuGetVersion.Parse("2.0.2");
            IReadOnlyList<CommandSettings> listOfCommandSettings2 = new[]
            {
                new CommandSettings("tool1", "dotnet", nuGetGlobalPackagesFolder.WithFile("tool1new.dll")),
                new CommandSettings("tool2", "dotnet", nuGetGlobalPackagesFolder.WithFile("tool2new.dll")),
                new CommandSettings("tool3", "dotnet", nuGetGlobalPackagesFolder.WithFile("tool3new.dll"))
            };

            localToolsResolverCache.Save(packageId, nuGetVersion, targetFramework, runtimeIdentifier,
                listOfCommandSettings, nuGetGlobalPackagesFolder);
            localToolsResolverCache.Save(packageId, newerNuGetVersion, targetFramework, runtimeIdentifier,
                listOfCommandSettings2, nuGetGlobalPackagesFolder);

            IReadOnlyList<CommandSettings> loadedResolverCacheOlderVersion =
                localToolsResolverCache.Load(packageId, nuGetVersion, targetFramework, runtimeIdentifier,
                    nuGetGlobalPackagesFolder);
            IReadOnlyList<CommandSettings> loadedResolverCacheNewerVersion =
                localToolsResolverCache.Load(packageId, newerNuGetVersion, targetFramework, runtimeIdentifier,
                    nuGetGlobalPackagesFolder);

            loadedResolverCacheOlderVersion.Should().Contain(c =>
                c.Name == "tool1" && c.Runner == "dotnet" &&
                c.Executable.ToString() == nuGetGlobalPackagesFolder.WithFile("tool1.dll").ToString());
            loadedResolverCacheOlderVersion.Should().Contain(c =>
                c.Name == "tool2" && c.Runner == "dotnet" &&
                c.Executable.ToString() == nuGetGlobalPackagesFolder.WithFile("tool2.dll").ToString());
            
            loadedResolverCacheNewerVersion.Should().Contain(c =>
                c.Name == "tool1" && c.Runner == "dotnet" &&
                c.Executable.ToString() == nuGetGlobalPackagesFolder.WithFile("tool1new.dll").ToString());
            loadedResolverCacheNewerVersion.Should().Contain(c =>
                c.Name == "tool2" && c.Runner == "dotnet" &&
                c.Executable.ToString() == nuGetGlobalPackagesFolder.WithFile("tool2new.dll").ToString());
            loadedResolverCacheNewerVersion.Should().Contain(c =>
                c.Name == "tool3" && c.Runner == "dotnet" &&
                c.Executable.ToString() == nuGetGlobalPackagesFolder.WithFile("tool3new.dll").ToString());
        }
        
        private static (DirectoryPath nuGetGlobalPackagesFolder, LocalToolsResolverCache localToolsResolverCache) Setup()
        {
            IFileSystem fileSystem = new FileSystemMockBuilder().UseCurrentSystemTemporaryDirectory().Build();
            DirectoryPath tempDirectory =
                new DirectoryPath(fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath);
            DirectoryPath cacheDirectory = tempDirectory.WithSubDirectories("cacheDirectory");
            DirectoryPath nuGetGlobalPackagesFolder = tempDirectory.WithSubDirectories("nugetGlobalPackageLocation");
            fileSystem.Directory.CreateDirectory(cacheDirectory.Value);
            const int version = 1;

            LocalToolsResolverCache localToolsResolverCache =
                new LocalToolsResolverCache(fileSystem, cacheDirectory, version);
            return (nuGetGlobalPackagesFolder, localToolsResolverCache);
        }
    }

    // TODO WUL nochecin move to a different file
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
