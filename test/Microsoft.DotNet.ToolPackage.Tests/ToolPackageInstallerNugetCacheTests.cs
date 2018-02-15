// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Transactions;
using FluentAssertions;
using Microsoft.DotNet.Tools.Test.Utilities;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools;
using Microsoft.DotNet.Tools.Install.Tool;
using Microsoft.DotNet.Tools.Tests.ComponentMocks;
using Microsoft.Extensions.DependencyModel.Tests;
using Microsoft.Extensions.EnvironmentAbstractions;
using Xunit;

namespace Microsoft.DotNet.ToolPackage.Tests
{
    public class ToolPackageNugetCacheInstaller : TestBase
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GivenNugetConfigInstallSucceeds(bool testMockBehaviorIsInSync)
        {
            var nugetConfigPath = WriteNugetConfigFileToPointToTheFeed();

            var (store, installer, reporter, fileSystem) = Setup(
                useMock: testMockBehaviorIsInSync,
                feeds: GetMockFeedsForConfigFile(nugetConfigPath));

            var nugetCacheLocation =
                new DirectoryPath(Path.GetTempPath()).WithSubDirectories(Path.GetRandomFileName());
            
            IReadOnlyList<CommandSettings> commands = installer.InstallPackageToNuGetCache(
                new NuGetPackageLocation(packageId: TestPackageId, packageVersion: TestPackageVersion,
                    nugetConfig: nugetConfigPath), targetFramework: _testTargetframework, nugetCacheLocation: nugetCacheLocation);

            fileSystem.File.Exists(commands[0].Executable.Value).Should().BeTrue($"{commands[0].Executable.Value} should exist");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GivenNugetConfigInstallSucceedsAndAssetsStoredInNuGetCache(bool testMockBehaviorIsInSync)
        {
            var nugetConfigPath = WriteNugetConfigFileToPointToTheFeed();

            var (store, installer, reporter, fileSystem) = Setup(
                useMock: testMockBehaviorIsInSync,
                feeds: GetMockFeedsForConfigFile(nugetConfigPath));

            var nugetCacheLocation =
                new DirectoryPath(Path.GetTempPath()).WithSubDirectories(Path.GetRandomFileName());

            installer.InstallPackageToNuGetCache(
                new NuGetPackageLocation(packageId: TestPackageId, packageVersion: TestPackageVersion,
                    nugetConfig: nugetConfigPath),
                targetFramework: _testTargetframework,
                nugetCacheLocation: nugetCacheLocation);

            Directory.Exists(nugetCacheLocation.WithSubDirectories(TestPackageId).Value).Should().BeTrue();
        }

        private static FilePath GetUniqueTempProjectPathEachTest()
        {
            var tempProjectDirectory =
                new DirectoryPath(Path.GetTempPath()).WithSubDirectories(Path.GetRandomFileName());
            var tempProjectPath =
                tempProjectDirectory.WithFile(Path.GetRandomFileName() + ".csproj");
            return tempProjectPath;
        }

        private static IEnumerable<MockFeed> GetMockFeedsForConfigFile(FilePath nugetConfig)
        {
            return new MockFeed[]
            {
                new MockFeed
                {
                    Type = MockFeedType.ExplicitNugetConfig,
                    Uri = nugetConfig.Value,
                    Packages = new List<MockFeedPackage>
                    {
                        new MockFeedPackage
                        {
                            PackageId = TestPackageId,
                            Version = TestPackageVersion
                        }
                    }
                }
            };
        }

        private static (IToolPackageStore, IToolPackageInstaller, BufferedReporter, IFileSystem) Setup(
            bool useMock,
            IEnumerable<MockFeed> feeds = null,
            FilePath? tempProject = null,
            DirectoryPath? offlineFeed = null)
        {
            var root = new DirectoryPath(Path.Combine(Directory.GetCurrentDirectory(), Path.GetRandomFileName()));
            var reporter = new BufferedReporter();

            IFileSystem fileSystem;
            IToolPackageStore store;
            IToolPackageInstaller installer;
            if (useMock)
            {
                fileSystem = new FileSystemMockBuilder().Build();
                store = new ToolPackageStoreMock(root, fileSystem);
                installer = new ToolPackageInstallerMock(
                    fileSystem: fileSystem,
                    store: store,
                    projectRestorer: new ProjectRestorerMock(
                        fileSystem: fileSystem,
                        reporter: reporter,
                        feeds: feeds));
            }
            else
            {
                fileSystem = new FileSystemWrapper();
                store = new ToolPackageStore(root);
                installer = new ToolPackageInstaller(
                    store: store,
                    projectRestorer: new ProjectRestorer(reporter),
                    tempProject: tempProject ?? GetUniqueTempProjectPathEachTest(),
                    offlineFeed: offlineFeed ?? new DirectoryPath("does not exist"));
            }

            return (store, installer, reporter, fileSystem);
        }
        
        private static FilePath WriteNugetConfigFileToPointToTheFeed()
        {
            var nugetConfigName = "nuget.config";

            var tempPathForNugetConfigWithWhiteSpace =
                Path.Combine(Path.GetTempPath(),
                    Path.GetRandomFileName() + " " + Path.GetRandomFileName());
            Directory.CreateDirectory(tempPathForNugetConfigWithWhiteSpace);

            NuGetConfig.Write(
                directory: tempPathForNugetConfigWithWhiteSpace,
                configname: nugetConfigName,
                localFeedPath: GetTestLocalFeedPath());

            return new FilePath(Path.GetFullPath(Path.Combine(tempPathForNugetConfigWithWhiteSpace, nugetConfigName)));
        }

        private static string GetTestLocalFeedPath() => Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "TestAssetLocalNugetFeed");
        private readonly string _testTargetframework = BundledTargetFramework.GetTargetFrameworkMoniker();
        private const string TestPackageVersion = "1.0.4";
        private const string TestPackageId = "global.tool.console.demo";
    }
}
