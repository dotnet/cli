// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reflection;
using FluentAssertions;
using Microsoft.DotNet.Tools.Test.Utilities;
using Microsoft.Extensions.EnvironmentAbstractions;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Tools.Install.Tool;
using Xunit;
using Microsoft.DotNet.Tools.Tests.ComponentMocks;

namespace Microsoft.DotNet.ToolPackage.Tests
{
    public class ToolPackageObtainerTests : TestBase
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GivenNugetConfigAndPackageNameAndVersionAndTargetFrameworkWhenCallItCanDownloadThePackage(
            bool testMockBehaviorIsInSync)
        {
            FilePath nugetConfigPath = WriteNugetConfigFileToPointToTheFeed();
            var toolsPath = Path.Combine(Directory.GetCurrentDirectory(), Path.GetRandomFileName());

            var packageObtainer =
                ConstructDefaultPackageObtainer(toolsPath, testMockBehaviorIsInSync);

            ToolConfigurationAndExecutableDirectory toolConfigurationAndExecutableDirectory
                = packageObtainer.ObtainAndReturnExecutablePath(
                    packageId: TestPackageId,
                    packageVersion: TestPackageVersion,
                    nugetconfig: nugetConfigPath,
                    targetframework: _testTargetframework);

            FilePath executable = toolConfigurationAndExecutableDirectory
                .ExecutableDirectory
                .WithFile(
                    toolConfigurationAndExecutableDirectory
                        .Configuration
                        .ToolAssemblyEntryPoint);

            File.Exists(executable.Value)
                .Should()
                .BeTrue(executable + " should have the executable");

            File.Delete(executable.Value);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GivenNugetConfigAndPackageNameAndVersionAndTargetFrameworkWhenCallItCreateAssetFile(
            bool testMockBehaviorIsInSync)
        {
            var nugetConfigPath = WriteNugetConfigFileToPointToTheFeed();
            var toolsPath = Path.Combine(Directory.GetCurrentDirectory(), Path.GetRandomFileName());

            var packageObtainer =
                ConstructDefaultPackageObtainer(toolsPath, testMockBehaviorIsInSync);

            ToolConfigurationAndExecutableDirectory toolConfigurationAndExecutableDirectory =
                packageObtainer.ObtainAndReturnExecutablePath(
                    packageId: TestPackageId,
                    packageVersion: TestPackageVersion,
                    nugetconfig: nugetConfigPath,
                    targetframework: _testTargetframework);

            var assetJsonPath = toolConfigurationAndExecutableDirectory
                .ExecutableDirectory
                .GetParentPath()
                .GetParentPath()
                .GetParentPath()
                .GetParentPath()
                .GetParentPath()
                .WithFile("project.assets.json").Value;

            File.Exists(assetJsonPath)
                .Should()
                .BeTrue(assetJsonPath + " should be created");

            File.Delete(assetJsonPath);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GivenAllButNoNugetConfigFilePathItCanDownloadThePackage(bool testMockBehaviorIsInSync)
        {
            var uniqueTempProjectPath = GetUniqueTempProjectPathEachTest();
            var tempProjectDirectory = uniqueTempProjectPath.GetDirectoryPath();
            var nugetConfigPath = WriteNugetConfigFileToPointToTheFeed();
            var toolsPath = Path.Combine(Directory.GetCurrentDirectory(), Path.GetRandomFileName());

            Directory.CreateDirectory(tempProjectDirectory.Value);

            /*
             * In test, we don't want NuGet to keep look up, so we point current directory to nugetconfig.
             */

            Directory.SetCurrentDirectory(nugetConfigPath.GetDirectoryPath().Value);

            IToolPackageObtainer packageObtainer;
            if (testMockBehaviorIsInSync)
            {
                packageObtainer = new ToolPackageObtainerMock();
            }
            else
            {
                packageObtainer = new ToolPackageObtainer(
                    new DirectoryPath(toolsPath),
                    GetUniqueTempProjectPathEachTest,
                    new Lazy<string>(),
                    new PackageToProjectFileAdder(),
                    new ProjectRestorer());
            }

            ToolConfigurationAndExecutableDirectory toolConfigurationAndExecutableDirectory =
                packageObtainer.ObtainAndReturnExecutablePath(
                    packageId: TestPackageId,
                    packageVersion: TestPackageVersion,
                    targetframework: _testTargetframework);

            var executable = toolConfigurationAndExecutableDirectory
                .ExecutableDirectory
                .WithFile(
                    toolConfigurationAndExecutableDirectory
                        .Configuration
                        .ToolAssemblyEntryPoint);

            File.Exists(executable.Value)
                .Should()
                .BeTrue(executable + " should have the executable");

            File.Delete(executable.Value);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GivenAllButNoPackageVersionItCanDownloadThePackage(bool testMockBehaviorIsInSync)
        {
            var nugetConfigPath = WriteNugetConfigFileToPointToTheFeed();
            var toolsPath = Path.Combine(Directory.GetCurrentDirectory(), Path.GetRandomFileName());

            var packageObtainer =
                ConstructDefaultPackageObtainer(toolsPath, testMockBehaviorIsInSync);

            ToolConfigurationAndExecutableDirectory toolConfigurationAndExecutableDirectory =
                packageObtainer.ObtainAndReturnExecutablePath(
                    packageId: TestPackageId,
                    nugetconfig: nugetConfigPath,
                    targetframework: _testTargetframework);

            var executable = toolConfigurationAndExecutableDirectory
                .ExecutableDirectory
                .WithFile(
                    toolConfigurationAndExecutableDirectory
                        .Configuration
                        .ToolAssemblyEntryPoint);

            File.Exists(executable.Value)
                .Should()
                .BeTrue(executable + " should have the executable");

            File.Delete(executable.Value);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GivenAllButNoPackageVersionAndInvokeTwiceItShouldNotThrow(bool testMockBehaviorIsInSync)
        {
            var nugetConfigPath = WriteNugetConfigFileToPointToTheFeed();
            var toolsPath = Path.Combine(Directory.GetCurrentDirectory(), Path.GetRandomFileName());

            var packageObtainer =
                ConstructDefaultPackageObtainer(toolsPath, testMockBehaviorIsInSync);

            packageObtainer.ObtainAndReturnExecutablePath(
                packageId: TestPackageId,
                nugetconfig: nugetConfigPath,
                targetframework: _testTargetframework);

            Action secondCall = () => packageObtainer.ObtainAndReturnExecutablePath(
                packageId: TestPackageId,
                nugetconfig: nugetConfigPath,
                targetframework: _testTargetframework);

            secondCall.ShouldNotThrow();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GivenAllButNoTargetFrameworkItCanDownloadThePackage(bool testMockBehaviorIsInSync)
        {
            var nugetConfigPath = WriteNugetConfigFileToPointToTheFeed();
            var toolsPath = Path.Combine(Directory.GetCurrentDirectory(), Path.GetRandomFileName());

            IToolPackageObtainer packageObtainer;
            if (testMockBehaviorIsInSync)
            {
                packageObtainer = new ToolPackageObtainerMock();
            }
            else
            {
                packageObtainer = new ToolPackageObtainer(
                    new DirectoryPath(toolsPath),
                    GetUniqueTempProjectPathEachTest,
                    new Lazy<string>(() => BundledTargetFramework.GetTargetFrameworkMoniker()),
                    new PackageToProjectFileAdder(),
                    new ProjectRestorer());
            }

            ToolConfigurationAndExecutableDirectory toolConfigurationAndExecutableDirectory =
                packageObtainer.ObtainAndReturnExecutablePath(
                    packageId: TestPackageId,
                    packageVersion: TestPackageVersion,
                    nugetconfig: nugetConfigPath);

            var executable = toolConfigurationAndExecutableDirectory
                .ExecutableDirectory
                .WithFile(
                    toolConfigurationAndExecutableDirectory
                        .Configuration
                        .ToolAssemblyEntryPoint);

            File.Exists(executable.Value)
                .Should()
                .BeTrue(executable + " should have the executable");

            File.Delete(executable.Value);
        }

        [Fact]
        public void GivenNonExistentNugetConfigFileItThrows()
        {
            var toolsPath = Path.Combine(Directory.GetCurrentDirectory(), Path.GetRandomFileName());

            var packageObtainer =
                ConstructDefaultPackageObtainer(toolsPath);

            Action a = () => packageObtainer.ObtainAndReturnExecutablePath(
                packageId: TestPackageId,
                packageVersion: TestPackageVersion,
                nugetconfig: new FilePath("NonExistent.file"),
                targetframework: _testTargetframework);

            a.ShouldThrow<PackageObtainException>()
                .And
                .Message.Should().Contain("does not exist");
        }

        [Fact]
        public void GivenASourceItCanObtainThePackageFromThatSource()
        {
            var toolsPath = Path.Combine(Directory.GetCurrentDirectory(), Path.GetRandomFileName());

            var packageObtainer = ConstructDefaultPackageObtainer(toolsPath);
            var toolConfigurationAndExecutableDirectory = packageObtainer.ObtainAndReturnExecutablePath(
                packageId: TestPackageId,
                packageVersion: TestPackageVersion,
                targetframework: _testTargetframework,
                source: Path.Combine(
                            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                            "TestAssetLocalNugetFeed"));

            var executable = toolConfigurationAndExecutableDirectory
                .ExecutableDirectory
                .WithFile(
                    toolConfigurationAndExecutableDirectory
                        .Configuration
                        .ToolAssemblyEntryPoint);

            File.Exists(executable.Value)
                .Should()
                .BeTrue(executable + " should have the executable");
        }

        private static readonly Func<FilePath> GetUniqueTempProjectPathEachTest = () =>
        {
            var tempProjectDirectory =
                new DirectoryPath(Path.GetTempPath()).WithSubDirectories(Path.GetRandomFileName());
            var tempProjectPath =
                tempProjectDirectory.WithFile(Path.GetRandomFileName() + ".csproj");
            return tempProjectPath;
        };

        private static IToolPackageObtainer ConstructDefaultPackageObtainer(string toolsPath,
            bool testMockBehaviorIsInSync = false)
        {
            if (testMockBehaviorIsInSync)
            {
                return new ToolPackageObtainerMock();
            }

            return new ToolPackageObtainer(
                new DirectoryPath(toolsPath),
                GetUniqueTempProjectPathEachTest,
                new Lazy<string>(),
                new PackageToProjectFileAdder(),
                new ProjectRestorer());
        }

        private static FilePath WriteNugetConfigFileToPointToTheFeed()
        {
            var nugetConfigName = "nuget.config";
            var executeDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var tempPathForNugetConfigWithWhiteSpace =
                Path.Combine(Path.GetTempPath(),
                    Path.GetRandomFileName() + " " + Path.GetRandomFileName());
            Directory.CreateDirectory(tempPathForNugetConfigWithWhiteSpace);

            NuGetConfig.Write(
                directory: tempPathForNugetConfigWithWhiteSpace,
                configname: nugetConfigName,
                localFeedPath: Path.Combine(executeDirectory, "TestAssetLocalNugetFeed"));
            return new FilePath(Path.GetFullPath(Path.Combine(tempPathForNugetConfigWithWhiteSpace, nugetConfigName)));
        }

        private readonly string _testTargetframework = BundledTargetFramework.GetTargetFrameworkMoniker();
        private const string TestPackageVersion = "1.0.4";
        private const string TestPackageId = "global.tool.console.demo";
    }
}
