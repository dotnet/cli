// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;
using Xunit;
using Moq;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.Tools.Test.Utilities;
using Microsoft.Extensions.PlatformAbstractions;
using System.Threading;
using FluentAssertions;
using NuGet.Frameworks;

namespace Microsoft.DotNet.Cli.Utils.Tests
{
    public class GivenAProjectDependenciesCommandResolver
    {

        private static readonly string s_liveProjectDirectory = 
            Path.Combine(AppContext.BaseDirectory, "TestAssets/TestProjects/AppWithDirectDependency");

        [Fact]
        public void It_returns_null_when_CommandName_is_null()
        {
            var projectDependenciesCommandResolver = SetupProjectDependenciesCommandResolver();

            var commandResolverArguments = new CommandResolverArguments()
            {
                CommandName = null,
                CommandArguments = new string[] {""},
                ProjectDirectory = "/some/directory",
                Configuration = "Debug",
                Framework = FrameworkConstants.CommonFrameworks.NetStandardApp15
            };

            var result = projectDependenciesCommandResolver.Resolve(commandResolverArguments);

            result.Should().BeNull();
        }

        [Fact]
        public void It_returns_null_when_ProjectDirectory_is_null()
        {
            var projectDependenciesCommandResolver = SetupProjectDependenciesCommandResolver();

            var commandResolverArguments = new CommandResolverArguments()
            {
                CommandName = "command",
                CommandArguments = new string[] {""},
                ProjectDirectory = null,
                Configuration = "Debug",
                Framework = FrameworkConstants.CommonFrameworks.NetStandardApp15
            };

            var result = projectDependenciesCommandResolver.Resolve(commandResolverArguments);

            result.Should().BeNull();
        }

        [Fact]
        public void It_returns_null_when_Framework_is_null()
        {
            var projectDependenciesCommandResolver = SetupProjectDependenciesCommandResolver();

            var commandResolverArguments = new CommandResolverArguments()
            {
                CommandName = "command",
                CommandArguments = new string[] {""},
                ProjectDirectory = s_liveProjectDirectory,
                Configuration = "Debug",
                Framework = null
            };

            var result = projectDependenciesCommandResolver.Resolve(commandResolverArguments);

            result.Should().BeNull();
        }

        [Fact]
        public void It_returns_null_when_Configuration_is_null()
        {
            var projectDependenciesCommandResolver = SetupProjectDependenciesCommandResolver();

            var commandResolverArguments = new CommandResolverArguments()
            {
                CommandName = "command",
                CommandArguments = new string[] {""},
                ProjectDirectory = s_liveProjectDirectory,
                Configuration = null,
                Framework = FrameworkConstants.CommonFrameworks.NetStandardApp15
            };

            var result = projectDependenciesCommandResolver.Resolve(commandResolverArguments);

            result.Should().BeNull();
        }

        [Fact]
        public void It_returns_null_when_CommandName_does_not_exist_in_ProjectDependencies()
        {
            var projectDependenciesCommandResolver = SetupProjectDependenciesCommandResolver();

            var commandResolverArguments = new CommandResolverArguments()
            {
                CommandName = "nonexistent-command",
                CommandArguments = null,
                ProjectDirectory = s_liveProjectDirectory,
                Configuration = "Debug",
                Framework = FrameworkConstants.CommonFrameworks.NetStandardApp15
            };

            var result = projectDependenciesCommandResolver.Resolve(commandResolverArguments);

            result.Should().BeNull();
        }

        [Fact]
        public void It_returns_a_CommandSpec_with_CoreHost_as_FileName_and_CommandName_in_Args_when_CommandName_exists_in_ProjectDependencies()
        {
            var projectDependenciesCommandResolver = SetupProjectDependenciesCommandResolver();

            var commandResolverArguments = new CommandResolverArguments()
            {
                CommandName = "dotnet-hello",
                CommandArguments = null,
                ProjectDirectory = s_liveProjectDirectory,
                Configuration = "Debug",
                Framework = FrameworkConstants.CommonFrameworks.NetStandardApp15
            };

            var result = projectDependenciesCommandResolver.Resolve(commandResolverArguments);

            result.Should().NotBeNull();

            var commandFile = Path.GetFileNameWithoutExtension(result.Path);

            commandFile.Should().Be("corehost");

            result.Args.Should().Contain(commandResolverArguments.CommandName);
        }

        [Fact]
        public void It_escapes_CommandArguments_when_returning_a_CommandSpec()
        {
            var projectDependenciesCommandResolver = SetupProjectDependenciesCommandResolver();

            var commandResolverArguments = new CommandResolverArguments()
            {
                CommandName = "dotnet-hello",
                CommandArguments = new [] { "arg with space"},
                ProjectDirectory = s_liveProjectDirectory,
                Configuration = "Debug",
                Framework = FrameworkConstants.CommonFrameworks.NetStandardApp15
            };

            var result = projectDependenciesCommandResolver.Resolve(commandResolverArguments);

            result.Should().NotBeNull();
            result.Args.Should().Contain("\"arg with space\"");
        }

        [Fact]
        public void It_passes_depsfile_arg_to_corehost_when_returning_a_commandspec()
        {
            var projectDependenciesCommandResolver = SetupProjectDependenciesCommandResolver();

            var commandResolverArguments = new CommandResolverArguments()
            {
                CommandName = "dotnet-hello",
                CommandArguments = null,
                ProjectDirectory = s_liveProjectDirectory,
                Configuration = "Debug",
                Framework = FrameworkConstants.CommonFrameworks.NetStandardApp15
            };

            var result = projectDependenciesCommandResolver.Resolve(commandResolverArguments);

            result.Should().NotBeNull();
            result.Args.Should().Contain("--depsfile");
        }

        [Fact]
        public void It_sets_depsfile_based_on_output_path_when_returning_a_commandspec()
        {
            var projectDependenciesCommandResolver = SetupProjectDependenciesCommandResolver();

            var commandResolverArguments = new CommandResolverArguments
            {
                CommandName = "dotnet-hello",
                CommandArguments = null,
                ProjectDirectory = s_liveProjectDirectory,
                Configuration = "Debug",
                Framework = FrameworkConstants.CommonFrameworks.NetStandardApp15,
                OutputPath = AppContext.BaseDirectory
            };

            var projectContext = ProjectContext.Create(
                s_liveProjectDirectory,
                FrameworkConstants.CommonFrameworks.NetStandardApp15,
                PlatformServices.Default.Runtime.GetAllCandidateRuntimeIdentifiers());

            var depsFilePath =
                projectContext.GetOutputPaths("Debug", outputPath: AppContext.BaseDirectory).RuntimeFiles.DepsJson;

            var result = projectDependenciesCommandResolver.Resolve(commandResolverArguments);

            result.Should().NotBeNull();
            result.Args.Should().Contain($"--depsfile {depsFilePath}");
        }

        [Fact]
        public void It_sets_depsfile_based_on_build_base_path_when_returning_a_commandspec()
        {
            var projectDependenciesCommandResolver = SetupProjectDependenciesCommandResolver();

            var commandResolverArguments = new CommandResolverArguments
            {
                CommandName = "dotnet-hello",
                CommandArguments = null,
                ProjectDirectory = s_liveProjectDirectory,
                Configuration = "Debug",
                Framework = FrameworkConstants.CommonFrameworks.NetStandardApp15,
                BuildBasePath = AppContext.BaseDirectory
            };

            var projectContext = ProjectContext.Create(
                s_liveProjectDirectory,
                FrameworkConstants.CommonFrameworks.NetStandardApp15,
                PlatformServices.Default.Runtime.GetAllCandidateRuntimeIdentifiers());

            var depsFilePath =
                projectContext.GetOutputPaths("Debug", AppContext.BaseDirectory).RuntimeFiles.DepsJson;

            var result = projectDependenciesCommandResolver.Resolve(commandResolverArguments);

            result.Should().NotBeNull();
            result.Args.Should().Contain($"--depsfile {depsFilePath}");
        }

        [Fact]
        public void It_returns_a_CommandSpec_with_CommandName_in_Args_when_returning_a_CommandSpec_and_CommandArguments_are_null()
        {
            var projectDependenciesCommandResolver = SetupProjectDependenciesCommandResolver();

            var commandResolverArguments = new CommandResolverArguments()
            {
                CommandName = "dotnet-hello",
                CommandArguments = null,
                ProjectDirectory = s_liveProjectDirectory,
                Configuration = "Debug",
                Framework = FrameworkConstants.CommonFrameworks.NetStandardApp15
            };

            var result = projectDependenciesCommandResolver.Resolve(commandResolverArguments);

            result.Should().NotBeNull();
            
            result.Args.Should().Contain("dotnet-hello");
        }

        private ProjectDependenciesCommandResolver SetupProjectDependenciesCommandResolver(
            IEnvironmentProvider environment = null,
            IPackagedCommandSpecFactory packagedCommandSpecFactory = null)
        {
            environment = environment ?? new EnvironmentProvider();
            packagedCommandSpecFactory = packagedCommandSpecFactory ?? new PackagedCommandSpecFactory();

            var projectDependenciesCommandResolver = new ProjectDependenciesCommandResolver(environment, packagedCommandSpecFactory);

            return projectDependenciesCommandResolver;
        }
    }
}
