// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using FluentAssertions;
using Microsoft.DotNet.Tools.Test.Utilities;
using Microsoft.Extensions.EnvironmentAbstractions;
using Microsoft.DotNet.Cli;
using Xunit;

namespace Microsoft.DotNet.ExecutablePackageObtainer.Tests
{
    public class ExecutablePackageObtainerTests : TestBase
    {
        [Fact]
        public void GivenNugetConfigAndPackageNameAndVersionAndTargetFrameworkWhenCallItCanDownloadThePacakge()
        {
            FilePath nugetConfigPath = WriteNugetConfigFileToPointToTheFeed();
            var toolsPath = Path.Combine(Directory.GetCurrentDirectory(), Path.GetRandomFileName());

            ExecutablePackageObtainer packageObtainer =
                ConstructDefaultPackageObtainer(toolsPath);
            ToolConfigurationAndExecutableDirectory toolConfigurationAndExecutableDirectory = packageObtainer.ObtainAndReturnExecutablePath(
                packageId: TestPackageId,
                packageVersion: TestPackageVersion,
                nugetconfig: nugetConfigPath,
                targetframework: _testTargetframework);

            var executable = toolConfigurationAndExecutableDirectory
                .ExecutableDirectory
                .CreateFilePathWithCombineFollowing(
                    toolConfigurationAndExecutableDirectory
                        .Configuration
                        .ToolAssemblyEntryPoint);

            File.Exists(executable.Value)
                .Should()
                .BeTrue(executable + " should have the executable");
        }

        [Fact]
        public void GivenNugetConfigAndPackageNameAndVersionAndTargetFrameworkWhenCallItCreateAssetFile()
        {
            var nugetConfigPath = WriteNugetConfigFileToPointToTheFeed();
            var toolsPath = Path.Combine(Directory.GetCurrentDirectory(), Path.GetRandomFileName());

            ExecutablePackageObtainer packageObtainer =
                ConstructDefaultPackageObtainer(toolsPath);
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
                .CreateFilePathWithCombineFollowing("project.assets.json").Value;

            File.Exists(assetJsonPath)
                .Should()
                .BeTrue(assetJsonPath + " should be created");
        }

        [Fact]
        public void GivenAllButNoNugetConfigFilePathtCanDownloadThePacakge()
        {
            var uniqueTempProjectPath = GetUniqueTempProjectPathEachTest();
            var tempProjectDirectory = uniqueTempProjectPath.GetDirectoryPath();
            var nugetConfigPath = WriteNugetConfigFileToPointToTheFeed();
            var toolsPath = Path.Combine(Directory.GetCurrentDirectory(), Path.GetRandomFileName());

            Directory.CreateDirectory(tempProjectDirectory.Value);
            File.Copy(nugetConfigPath.Value,
                tempProjectDirectory.CreateFilePathWithCombineFollowing("nuget.config").Value);

            var packageObtainer =
                new ExecutablePackageObtainer(
                    new DirectoryPath(toolsPath),
                    () => uniqueTempProjectPath,
                    new Lazy<string>(),
                    new PackageToProjectFileAdder(),
                    new ProjectRestorer());
            ToolConfigurationAndExecutableDirectory toolConfigurationAndExecutableDirectory = packageObtainer.ObtainAndReturnExecutablePath(
                packageId: TestPackageId,
                packageVersion: TestPackageVersion,
                targetframework: _testTargetframework);

            var executable = toolConfigurationAndExecutableDirectory
                .ExecutableDirectory
                .CreateFilePathWithCombineFollowing(
                    toolConfigurationAndExecutableDirectory
                        .Configuration
                        .ToolAssemblyEntryPoint);

            File.Exists(executable.Value)
                .Should()
                .BeTrue(executable + " should have the executable");
        }

        [Fact]
        public void GivenAllButNoPackageVersionItCanDownloadThePacakge()
        {
            var nugetConfigPath = WriteNugetConfigFileToPointToTheFeed();
            var toolsPath = Path.Combine(Directory.GetCurrentDirectory(), Path.GetRandomFileName());

            ExecutablePackageObtainer packageObtainer =
                ConstructDefaultPackageObtainer(toolsPath);
            ToolConfigurationAndExecutableDirectory toolConfigurationAndExecutableDirectory = packageObtainer.ObtainAndReturnExecutablePath(
                packageId: TestPackageId,
                nugetconfig: nugetConfigPath,
                targetframework: _testTargetframework);

            var executable = toolConfigurationAndExecutableDirectory
                .ExecutableDirectory
                .CreateFilePathWithCombineFollowing(
                    toolConfigurationAndExecutableDirectory
                        .Configuration
                        .ToolAssemblyEntryPoint);

            File.Exists(executable.Value)
                .Should()
                .BeTrue(executable + " should have the executable");
        }

        [Fact]
        public void GivenAllButNoTargetFrameworkItCanDownloadThePacakge()
        {
            var nugetConfigPath = WriteNugetConfigFileToPointToTheFeed();
            var toolsPath = Path.Combine(Directory.GetCurrentDirectory(), Path.GetRandomFileName());

            var packageObtainer =
                new ExecutablePackageObtainer(
                    new DirectoryPath(toolsPath),
                    GetUniqueTempProjectPathEachTest,
                    new Lazy<string>(() => BundledTargetFramework.TargetFrameworkMoniker),
                    new PackageToProjectFileAdder(),
                    new ProjectRestorer());
            ToolConfigurationAndExecutableDirectory toolConfigurationAndExecutableDirectory =
                packageObtainer.ObtainAndReturnExecutablePath(
                    packageId: TestPackageId,
                    packageVersion: TestPackageVersion,
                    nugetconfig: nugetConfigPath);

            var executable = toolConfigurationAndExecutableDirectory
                .ExecutableDirectory
                .CreateFilePathWithCombineFollowing(
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
                new DirectoryPath(Path.GetTempPath()).WithCombineFollowing(Path.GetRandomFileName());
            var tempProjectPath =
                tempProjectDirectory.CreateFilePathWithCombineFollowing(Path.GetRandomFileName() + ".csproj");
            return tempProjectPath;
        };

        private static ExecutablePackageObtainer ConstructDefaultPackageObtainer(string toolsPath)
        {
            return new ExecutablePackageObtainer(
                new DirectoryPath(toolsPath),
                GetUniqueTempProjectPathEachTest,
                new Lazy<string>(),
                new PackageToProjectFileAdder(),
                new ProjectRestorer());
        }

        private static FilePath WriteNugetConfigFileToPointToTheFeed()
        {
            var nugetConfigName = Path.GetRandomFileName() + ".config";
            var executeDirectory =
                Path.GetDirectoryName(
                    System.Reflection
                        .Assembly
                        .GetExecutingAssembly()
                        .Location);
            NuGetConfig.Write(
                directory: executeDirectory,
                configname: nugetConfigName,
                localFeedPath: Path.Combine(executeDirectory, "TestAssetLocalNugetFeed"));
            return new FilePath(Path.GetFullPath(nugetConfigName));
        }

        private readonly string _testTargetframework = BundledTargetFramework.TargetFrameworkMoniker;
        private const string TestPackageVersion = "1.0.4";
        private const string TestPackageId = "global.tool.console.demo";
    }
}
