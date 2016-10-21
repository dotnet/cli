// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using FluentAssertions;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.InternalAbstractions;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.TestFramework;
using Microsoft.DotNet.Tools.Test.Utilities;
using NuGet.Frameworks;
using Xunit;

namespace Microsoft.DotNet.Cli.Utils.Tests
{
    public class GivenAProjectDependencyCommandResolver : TestBase
    {
        private TestInstance MSBuildTestProjectInstance;

        public GivenAProjectDependencyCommandResolver()
        {
            Environment.SetEnvironmentVariable(
                Constants.MSBUILD_EXE_PATH,
                Path.Combine(new RepoDirectoriesProvider().Stage2Sdk, "MSBuild.dll"));
        }

        [Fact]
        public void ItReturnsACommandSpecWithDotnetAsFileNameAndCommandNameInArgsWhenCommandNameExistsInMSBuildProjectDependencies()
        {
            MSBuildTestProjectInstance =
                TestAssetsManager.CreateTestInstance("MSBuildTestAppWithToolInDependencies")
                    .WithBuildArtifacts()
                    .WithNuGetMSBuildFiles()
                    .WithLockFiles();

            var projectDependenciesCommandResolver = SetupProjectDependenciesCommandResolver();

            var commandResolverArguments = new CommandResolverArguments()
            {
                CommandName = "dotnet-portable",
                ProjectDirectory = MSBuildTestProjectInstance.Path,
                Framework = FrameworkConstants.CommonFrameworks.NetCoreApp10
            };

            var result = projectDependenciesCommandResolver.Resolve(commandResolverArguments);

            result.Should().NotBeNull();

            var commandFile = Path.GetFileNameWithoutExtension(result.Path);

            commandFile.Should().Be("dotnet");

            result.Args.Should().Contain(commandResolverArguments.CommandName);
        }

        [Fact]
        public void ItPassesDepsfileArgToHostWhenReturningACommandSpecForMSBuildProject()
        {
            MSBuildTestProjectInstance =
                TestAssetsManager.CreateTestInstance("MSBuildTestAppWithToolInDependencies")
                    .WithNuGetMSBuildFiles()
                    .WithLockFiles();

            var projectDependenciesCommandResolver = SetupProjectDependenciesCommandResolver();

            var commandResolverArguments = new CommandResolverArguments()
            {
                CommandName = "dotnet-portable",
                CommandArguments = null,
                ProjectDirectory = MSBuildTestProjectInstance.Path,
                Framework = FrameworkConstants.CommonFrameworks.NetCoreApp10
            };

            var result = projectDependenciesCommandResolver.Resolve(commandResolverArguments);

            result.Should().NotBeNull();
            result.Args.Should().Contain("--depsfile");
        }

        [Fact]
        public void ItReturnsNullWhenCommandNameDoesNotExistInProjectDependenciesForMSBuildProject()
        {
            MSBuildTestProjectInstance =
                TestAssetsManager.CreateTestInstance("MSBuildTestAppWithToolInDependencies")
                    .WithNuGetMSBuildFiles()
                    .WithLockFiles();

            var projectDependenciesCommandResolver = SetupProjectDependenciesCommandResolver();

            var commandResolverArguments = new CommandResolverArguments()
            {
                CommandName = "nonexistent-command",
                CommandArguments = null,
                ProjectDirectory = MSBuildTestProjectInstance.Path,
                Framework = FrameworkConstants.CommonFrameworks.NetCoreApp10
            };

            var result = projectDependenciesCommandResolver.Resolve(commandResolverArguments);

            result.Should().BeNull();
        }

        [Fact]
        public void ItSetsDepsfileToOutputInCommandspecForMSBuild()
        {
            MSBuildTestProjectInstance =
                TestAssetsManager.CreateTestInstance("MSBuildTestAppWithToolInDependencies")
                    .WithNuGetMSBuildFiles()
                    .WithLockFiles();

            var projectDependenciesCommandResolver = SetupProjectDependenciesCommandResolver();

            var testInstance = TestAssetsManager.CreateTestInstance("MSBuildTestAppWithToolInDependencies");

            var outputDir = Path.Combine(testInstance.Path, "out");

            var commandResolverArguments = new CommandResolverArguments()
            {
                CommandName = "dotnet-portable",
                CommandArguments = null,
                ProjectDirectory = testInstance.Path,
                Framework = FrameworkConstants.CommonFrameworks.NetCoreApp10,
                OutputPath = outputDir
            };

            new BuildCommand()
                .WithWorkingDirectory(testInstance.Path)
                .Execute($"-o {outputDir}")
                .Should()
                .Pass();

            var result = projectDependenciesCommandResolver.Resolve(commandResolverArguments);

            var depsFilePath = Path.Combine(outputDir, "MSBuildTestAppWithToolInDependencies.deps.json");

            result.Should().NotBeNull();
            result.Args.Should().Contain($"--depsfile {depsFilePath}");
        }

        private ProjectDependenciesCommandResolver SetupProjectDependenciesCommandResolver(
            IEnvironmentProvider environment = null,
            IPackagedCommandSpecFactory packagedCommandSpecFactory = null)
        {
            Environment.SetEnvironmentVariable(
                Constants.MSBUILD_EXE_PATH,
                Path.Combine(new RepoDirectoriesProvider().Stage2Sdk, "MSBuild.dll"));

            environment = environment ?? new EnvironmentProvider();
            packagedCommandSpecFactory = packagedCommandSpecFactory ?? new PackagedCommandSpecFactory();

            var projectDependenciesCommandResolver = new ProjectDependenciesCommandResolver(environment, packagedCommandSpecFactory);

            return projectDependenciesCommandResolver;
        }
    }
}
