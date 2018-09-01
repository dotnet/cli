// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
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
        private static
            (DirectoryPath nuGetGlobalPackagesFolder,
            LocalToolsResolverCache localToolsResolverCache) Setup()
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

        [Fact(Skip = "Pending")]
        public void GivenDifferentResolverCacheVersionItCannotSaveAndLoad()
        {
        }


        [Fact]
        public void GivenExecutableIdentifierItCanSaveAndCannotLoadWithMismatches()
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

            localToolsResolverCache.Save(
                new CommandSettingsListId(packageId, nuGetVersion, targetFramework, runtimeIdentifier),
                listOfCommandSettings, nuGetGlobalPackagesFolder);

            localToolsResolverCache
                .Load(
                    new CommandSettingsListId(packageId, NuGetVersion.Parse("1.0.0-wrong-version"), targetFramework,
                        runtimeIdentifier), nuGetGlobalPackagesFolder)
                .Should().BeEmpty();

            localToolsResolverCache
                .Load(
                    new CommandSettingsListId(packageId, nuGetVersion, NuGetFramework.Parse("wrongFramework"),
                        runtimeIdentifier), nuGetGlobalPackagesFolder)
                .Should().BeEmpty();

            localToolsResolverCache
                .Load(new CommandSettingsListId(packageId, nuGetVersion, targetFramework, "wrongRuntimeIdentifier"),
                    nuGetGlobalPackagesFolder)
                .Should().BeEmpty();
        }

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

            localToolsResolverCache.Save(
                new CommandSettingsListId(packageId, nuGetVersion, targetFramework, runtimeIdentifier),
                listOfCommandSettings, nuGetGlobalPackagesFolder);

            IReadOnlyList<CommandSettings> loadedResolverCache =
                localToolsResolverCache.Load(
                    new CommandSettingsListId(packageId, nuGetVersion, targetFramework, runtimeIdentifier),
                    nuGetGlobalPackagesFolder);

            loadedResolverCache.Should().ContainSingle(c =>
                c.Name == "tool1" && c.Runner == "dotnet" &&
                c.Executable.ToString() == nuGetGlobalPackagesFolder.WithFile("tool1.dll").ToString());
            loadedResolverCache.Should().ContainSingle(c =>
                c.Name == "tool2" && c.Runner == "dotnet" &&
                c.Executable.ToString() == nuGetGlobalPackagesFolder.WithFile("tool2.dll").ToString());
        }

        [Fact]
        public void GivenExecutableIdentifierItCanSaveMultipleSameAndLoadContainsOnlyOne()
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

            localToolsResolverCache.Save(
                new CommandSettingsListId(packageId, nuGetVersion, targetFramework, runtimeIdentifier),
                listOfCommandSettings, nuGetGlobalPackagesFolder);
            localToolsResolverCache.Save(
                new CommandSettingsListId(packageId, nuGetVersion, targetFramework, runtimeIdentifier),
                listOfCommandSettings, nuGetGlobalPackagesFolder);

            IReadOnlyList<CommandSettings> loadedResolverCache =
                localToolsResolverCache.Load(
                    new CommandSettingsListId(packageId, nuGetVersion, targetFramework, runtimeIdentifier),
                    nuGetGlobalPackagesFolder);

            loadedResolverCache.Should().ContainSingle(c =>
                c.Name == "tool1" && c.Runner == "dotnet" &&
                c.Executable.ToString() == nuGetGlobalPackagesFolder.WithFile("tool1.dll").ToString());
            loadedResolverCache.Should().ContainSingle(c =>
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

            localToolsResolverCache.Save(
                new CommandSettingsListId(packageId, nuGetVersion, targetFramework, runtimeIdentifier),
                listOfCommandSettings, nuGetGlobalPackagesFolder);
            localToolsResolverCache.Save(
                new CommandSettingsListId(packageId, newerNuGetVersion, targetFramework, runtimeIdentifier),
                listOfCommandSettings2, nuGetGlobalPackagesFolder);

            IReadOnlyList<CommandSettings> loadedResolverCacheOlderVersion =
                localToolsResolverCache.Load(
                    new CommandSettingsListId(packageId, nuGetVersion, targetFramework, runtimeIdentifier),
                    nuGetGlobalPackagesFolder);
            IReadOnlyList<CommandSettings> loadedResolverCacheNewerVersion =
                localToolsResolverCache.Load(
                    new CommandSettingsListId(packageId, newerNuGetVersion, targetFramework, runtimeIdentifier),
                    nuGetGlobalPackagesFolder);

            loadedResolverCacheOlderVersion.Should().ContainSingle(c =>
                c.Name == "tool1" && c.Runner == "dotnet" &&
                c.Executable.ToString() == nuGetGlobalPackagesFolder.WithFile("tool1.dll").ToString());
            loadedResolverCacheOlderVersion.Should().ContainSingle(c =>
                c.Name == "tool2" && c.Runner == "dotnet" &&
                c.Executable.ToString() == nuGetGlobalPackagesFolder.WithFile("tool2.dll").ToString());

            loadedResolverCacheNewerVersion.Should().ContainSingle(c =>
                c.Name == "tool1" && c.Runner == "dotnet" &&
                c.Executable.ToString() == nuGetGlobalPackagesFolder.WithFile("tool1new.dll").ToString());
            loadedResolverCacheNewerVersion.Should().ContainSingle(c =>
                c.Name == "tool2" && c.Runner == "dotnet" &&
                c.Executable.ToString() == nuGetGlobalPackagesFolder.WithFile("tool2new.dll").ToString());
            loadedResolverCacheNewerVersion.Should().ContainSingle(c =>
                c.Name == "tool3" && c.Runner == "dotnet" &&
                c.Executable.ToString() == nuGetGlobalPackagesFolder.WithFile("tool3new.dll").ToString());
        }

        [Fact(Skip = "Pending")]
        public void GivenExecutableIdentifierRangeItCanSaveAndLoad()
        {
        }

        [Fact(Skip = "Pending")]
        public void ItShouldHandlePotentialCorruption()
        {
        }
    }
}
