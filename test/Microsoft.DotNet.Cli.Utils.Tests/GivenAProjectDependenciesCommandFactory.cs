// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using FluentAssertions;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.TestFramework;
using Microsoft.DotNet.Tools.Test.Utilities;
using NuGet.Frameworks;
using Xunit;

namespace Microsoft.DotNet.Cli.Utils.Tests
{
    public class GivenAProjectDependenciesCommandFactory : TestBase
    {
        private static readonly NuGetFramework s_desktopTestFramework = FrameworkConstants.CommonFrameworks.Net451;

        private RepoDirectoriesProvider _repoDirectoriesProvider;

        public GivenAProjectDependenciesCommandFactory()
        {
            _repoDirectoriesProvider = new RepoDirectoriesProvider();
            Environment.SetEnvironmentVariable(
                Constants.MSBUILD_EXE_PATH,
                Path.Combine(_repoDirectoriesProvider.Stage2Sdk, "MSBuild.dll"));
        }

        [WindowsOnlyFact]
        public void It_resolves_desktop_apps_defaulting_to_Debug_Configuration()
        {
            var configuration = "Debug";

            var testAssetManager = new TestAssetsManager(Path.Combine(RepoRoot, "TestAssets", "DesktopTestProjects"));
            var testInstance = testAssetManager
                .CreateTestInstance("AppWithDirectDepDesktopAndPortable")
                .WithLockFiles();

            var buildCommand = new BuildCommand()
                .WithProjectFile(new FileInfo(Path.Combine(testInstance.TestRoot, "project.json")))
                .WithConfiguration(configuration)
                .WithCapturedOutput()
                .Execute()
                .Should().Pass();

            var context = ProjectContext.Create(testInstance.TestRoot, s_desktopTestFramework);

            var factory = new ProjectDependenciesCommandFactory(
                s_desktopTestFramework,
                null,
                null,
                null,
                testInstance.TestRoot);

            var command = factory.Create("dotnet-desktop-and-portable", null);

            command.CommandName.Should().Contain(Path.Combine(testInstance.TestRoot, "bin", configuration));
            Path.GetFileName(command.CommandName).Should().Be("dotnet-desktop-and-portable.exe");
        }

        [WindowsOnlyFact]
        public void It_resolves_desktop_apps_with_MSBuild_defaulting_to_Debug_Configuration()
        {
            var configuration = "Debug";

            var testAssetManager = new TestAssetsManager(Path.Combine(RepoRoot, "TestAssets", "TestProjects"));
            var testInstance = testAssetManager.CreateTestInstance("MSBuildAppWithMultipleFrameworksAndTools", "i")
                .WithLockFiles();

            var projectFile = Path.Combine(testInstance.TestRoot, "MSBuildAppWithMultipleFrameworksAndTools.csproj");

            new RestoreCommand()
                .ExecuteWithCapturedOutput($"{projectFile} -s {_repoDirectoriesProvider.TestPackages}")
                .Should()
                .Pass();

            new BuildCommand()
                .Execute($"{projectFile} --configuration {configuration}")
                .Should()
                .Pass();

            var factory = new ProjectDependenciesCommandFactory(
                s_desktopTestFramework,
                null,
                null,
                null,
                testInstance.TestRoot);

            var command = factory.Create("dotnet-desktop-and-portable", null);

            command.CommandName.Should().Contain(Path.Combine(testInstance.TestRoot, "bin", configuration));
            Path.GetFileName(command.CommandName).Should().Be("dotnet-desktop-and-portable.exe");
        }

        [WindowsOnlyFact]
        public void It_resolves_desktop_apps_when_configuration_is_Debug()
        {
            var configuration = "Debug";

            var testAssetManager = new TestAssetsManager(Path.Combine(RepoRoot, "TestAssets", "DesktopTestProjects"));

            var testInstance = testAssetManager
                .CreateTestInstance("AppWithDirectDepDesktopAndPortable")
                .WithLockFiles();

            var buildCommand = new BuildCommand()
                .WithProjectFile(new FileInfo(Path.Combine(testInstance.TestRoot, "project.json")))
                .WithConfiguration(configuration)
                .WithCapturedOutput()
                .Execute()
                .Should().Pass();

            var context = ProjectContext.Create(testInstance.TestRoot, s_desktopTestFramework);

            var factory = new ProjectDependenciesCommandFactory(
                s_desktopTestFramework,
                configuration,
                null,
                null,
                testInstance.TestRoot);

            var command = factory.Create("dotnet-desktop-and-portable", null);

            command.CommandName.Should().Contain(Path.Combine(testInstance.TestRoot, "bin", configuration));
            Path.GetFileName(command.CommandName).Should().Be("dotnet-desktop-and-portable.exe");
        }

        [WindowsOnlyFact]
        public void It_resolves_desktop_apps_when_configuration_is_Release()
        {
            var configuration = "Release";

            var testAssetManager = new TestAssetsManager(Path.Combine(RepoRoot, "TestAssets", "DesktopTestProjects"));
            var testInstance = testAssetManager
                .CreateTestInstance("AppWithDirectDepDesktopAndPortable")
                .WithLockFiles();

            var buildCommand = new BuildCommand()
                .WithProjectFile(new FileInfo(Path.Combine(testInstance.TestRoot, "project.json")))
                .WithConfiguration(configuration)
                .WithCapturedOutput()
                .Execute()
                .Should().Pass();

            var context = ProjectContext.Create(testInstance.TestRoot, s_desktopTestFramework);

            var factory = new ProjectDependenciesCommandFactory(
                s_desktopTestFramework,
                configuration,
                null,
                null,
                testInstance.TestRoot);

            var command = factory.Create("dotnet-desktop-and-portable", null);

            command.CommandName.Should().Contain(Path.Combine(testInstance.TestRoot, "bin", configuration));
            Path.GetFileName(command.CommandName).Should().Be("dotnet-desktop-and-portable.exe");
        }

        [WindowsOnlyFact]
        public void It_resolves_desktop_apps_using_configuration_passed_to_create()
        {
            var configuration = "Release";

            var testAssetManager = new TestAssetsManager(Path.Combine(RepoRoot, "TestAssets", "DesktopTestProjects"));
            var testInstance = testAssetManager.CreateTestInstance("AppWithDirectDepDesktopAndPortable")
                .WithLockFiles();

            var buildCommand = new BuildCommand()
                .WithProjectFile(new FileInfo(Path.Combine(testInstance.TestRoot, "project.json")))
                .WithConfiguration(configuration)
                .WithCapturedOutput()
                .Execute()
                .Should().Pass();

            var context = ProjectContext.Create(testInstance.TestRoot, s_desktopTestFramework);

            var factory = new ProjectDependenciesCommandFactory(
                s_desktopTestFramework,
                "Debug",
                null,
                null,
                testInstance.TestRoot);

            var command = factory.Create("dotnet-desktop-and-portable", null, configuration: configuration);

            command.CommandName.Should().Contain(Path.Combine(testInstance.TestRoot, "bin", configuration));
            Path.GetFileName(command.CommandName).Should().Be("dotnet-desktop-and-portable.exe");
        }

        [Fact]
        public void It_resolves_tools_whose_package_name_is_different_than_dll_name()
        {
            Environment.SetEnvironmentVariable(
                Constants.MSBUILD_EXE_PATH,
                Path.Combine(new RepoDirectoriesProvider().Stage2Sdk, "MSBuild.dll"));

            var configuration = "Debug";

            var testAssetManager = new TestAssetsManager(Path.Combine(RepoRoot, "TestAssets", "TestProjects"));

            var testInstance = testAssetManager
                .CreateTestInstance("AppWithDirectDepWithOutputName")
                .WithNuGetMSBuildFiles() 
                .WithLockFiles();

            var buildCommand = new BuildCommand()
                .WithProjectDirectory(new DirectoryInfo(testInstance.TestRoot))
                .WithConfiguration(configuration)
                .WithCapturedOutput()
                .Execute()
                .Should().Pass();

            var factory = new ProjectDependenciesCommandFactory(
                FrameworkConstants.CommonFrameworks.NetCoreApp10,
                configuration,
                null,
                null,
                testInstance.TestRoot);

            var command = factory.Create("dotnet-tool-with-output-name", null);

            command.CommandArgs.Should().Contain(
                Path.Combine("toolwithoutputname", "1.0.0", "lib", "netcoreapp1.0", "dotnet-tool-with-output-name.dll"));
        }
    }
}
