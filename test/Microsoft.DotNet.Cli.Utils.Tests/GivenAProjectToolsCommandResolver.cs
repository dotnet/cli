// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.TestFramework;
using Microsoft.DotNet.Tools.Test.Utilities;
using NuGet.Frameworks;
using NuGet.ProjectModel;
using NuGet.Versioning;
using Xunit;

namespace Microsoft.DotNet.Tests
{
    public class GivenAProjectToolsCommandResolver : TestBase
    {
        private static readonly NuGetFramework s_toolPackageFramework =
            FrameworkConstants.CommonFrameworks.NetCoreApp20;

        private const string TestProjectName = "AppWithToolDependency";

        [Fact]
        public void ItReturnsNullWhenCommandNameIsNull()
        {
            var projectToolsCommandResolver = SetupProjectToolsCommandResolver();

            var commandResolverArguments = new CommandResolverArguments()
            {
                CommandName = null,
                CommandArguments = new string[] { "" },
                ProjectDirectory = "/some/directory"
            };

            var result = projectToolsCommandResolver.Resolve(commandResolverArguments);

            result.Should().BeNull();
        }

        [Fact]
        public void ItReturnsNullWhenProjectDirectoryIsNull()
        {
            var projectToolsCommandResolver = SetupProjectToolsCommandResolver();

            var commandResolverArguments = new CommandResolverArguments()
            {
                CommandName = "command",
                CommandArguments = new string[] { "" },
                ProjectDirectory = null
            };

            var result = projectToolsCommandResolver.Resolve(commandResolverArguments);

            result.Should().BeNull();
        }

        [Fact]
        public void ItReturnsNullWhenProjectDirectoryDoesNotContainAProjectFile()
        {
            var projectToolsCommandResolver = SetupProjectToolsCommandResolver();

            var projectDirectory = TestAssets.CreateTestDirectory();

            var commandResolverArguments = new CommandResolverArguments()
            {
                CommandName = "command",
                CommandArguments = new string[] { "" },
                ProjectDirectory = projectDirectory.Root.FullName
            };

            var result = projectToolsCommandResolver.Resolve(commandResolverArguments);

            result.Should().BeNull();
        }

        [Fact]
        public void ItReturnsNullWhenCommandNameDoesNotExistInProjectTools()
        {
            var projectToolsCommandResolver = SetupProjectToolsCommandResolver();

            var testInstance = TestAssets.Get(TestProjectName)
                .CreateInstance()
                .WithSourceFiles()
                .WithRestoreFiles();

            var commandResolverArguments = new CommandResolverArguments()
            {
                CommandName = "nonexistent-command",
                CommandArguments = null,
                ProjectDirectory = testInstance.Root.FullName
            };

            var result = projectToolsCommandResolver.Resolve(commandResolverArguments);

            result.Should().BeNull();
        }

        [Fact]
        public void ItReturnsACommandSpecWithDOTNETAsFileNameAndCommandNameInArgsWhenCommandNameExistsInProjectTools()
        {
            var projectToolsCommandResolver = SetupProjectToolsCommandResolver();

            var testInstance = TestAssets.Get(TestProjectName)
                .CreateInstance()
                .WithSourceFiles()
                .WithRestoreFiles();

            var commandResolverArguments = new CommandResolverArguments()
            {
                CommandName = "dotnet-portable",
                CommandArguments = null,
                ProjectDirectory = testInstance.Root.FullName
            };

            var result = projectToolsCommandResolver.Resolve(commandResolverArguments);

            result.Should().NotBeNull();

            var commandFile = Path.GetFileNameWithoutExtension(result.Path);

            commandFile.Should().Be("dotnet");

            result.Args.Should().Contain(commandResolverArguments.CommandName);
        }

        [Fact]
        public void ItEscapesCommandArgumentsWhenReturningACommandSpec()
        {
            var projectToolsCommandResolver = SetupProjectToolsCommandResolver();

            var testInstance = TestAssets.Get(TestProjectName)
                .CreateInstance()
                .WithSourceFiles()
                .WithRestoreFiles();

            var commandResolverArguments = new CommandResolverArguments()
            {
                CommandName = "dotnet-portable",
                CommandArguments = new[] { "arg with space" },
                ProjectDirectory = testInstance.Root.FullName
            };

            var result = projectToolsCommandResolver.Resolve(commandResolverArguments);

            result.Should().NotBeNull("Because the command is a project tool dependency");
            result.Args.Should().Contain("\"arg with space\"");
        }

        [Fact]
        public void ItReturnsACommandSpecWithArgsContainingCommandPathWhenReturningACommandSpecAndCommandArgumentsAreNull()
        {
            var projectToolsCommandResolver = SetupProjectToolsCommandResolver();

            var testInstance = TestAssets.Get(TestProjectName)
                .CreateInstance()
                .WithSourceFiles()
                .WithRestoreFiles();

            var commandResolverArguments = new CommandResolverArguments()
            {
                CommandName = "dotnet-portable",
                CommandArguments = null,
                ProjectDirectory = testInstance.Root.FullName
            };

            var result = projectToolsCommandResolver.Resolve(commandResolverArguments);

            result.Should().NotBeNull();

            var commandPath = result.Args.Trim('"');
            commandPath.Should().Contain("dotnet-portable.dll");
        }

        [Fact]
        public void ItReturnsACommandSpecWithArgsContainingCommandPathWhenInvokingAToolReferencedWithADifferentCasing()
        {
            var projectToolsCommandResolver = SetupProjectToolsCommandResolver();

            var testInstance = TestAssets.Get(TestProjectName)
                .CreateInstance()
                .WithSourceFiles()
                .WithRestoreFiles();

            var commandResolverArguments = new CommandResolverArguments()
            {
                CommandName = "dotnet-prefercliruntime",
                CommandArguments = null,
                ProjectDirectory = testInstance.Root.FullName
            };

            var result = projectToolsCommandResolver.Resolve(commandResolverArguments);

            result.Should().NotBeNull();

            var commandPath = result.Args.Trim('"');
            commandPath.Should().Contain("dotnet-prefercliruntime.dll");
        }

        [Fact]
        public void ItWritesADepsJsonFileNextToTheLockfile()
        {
            var projectToolsCommandResolver = SetupProjectToolsCommandResolver();

            var testInstance = TestAssets.Get(TestProjectName)
                .CreateInstance()
                .WithSourceFiles()
                .WithRestoreFiles();

            var commandResolverArguments = new CommandResolverArguments()
            {
                CommandName = "dotnet-portable",
                CommandArguments = null,
                ProjectDirectory = testInstance.Root.FullName
            };

            var repoDirectoriesProvider = new RepoDirectoriesProvider();

            var nugetPackagesRoot = repoDirectoriesProvider.NugetPackages;

            var toolPathCalculator = new ToolPathCalculator(nugetPackagesRoot);

            var lockFilePath = toolPathCalculator.GetLockFilePath(
                "dotnet-portable",
                new NuGetVersion("1.0.0"),
                s_toolPackageFramework);

            var directory = Path.GetDirectoryName(lockFilePath);

            var depsJsonFile = Directory
                .EnumerateFiles(directory)
                .FirstOrDefault(p => Path.GetFileName(p).EndsWith(FileNameSuffixes.DepsJson));

            if (depsJsonFile != null)
            {
                File.Delete(depsJsonFile);
            }

            var result = projectToolsCommandResolver.Resolve(commandResolverArguments);

            result.Should().NotBeNull();

            new DirectoryInfo(directory)
                .Should().HaveFilesMatching("*.deps.json", SearchOption.TopDirectoryOnly);
        }

        [Fact]
        public void GenerateDepsJsonMethodDoesntOverwriteWhenDepsFileAlreadyExists()
        {
            var testInstance = TestAssets.Get(TestProjectName)
                .CreateInstance()
                .WithSourceFiles()
                .WithRestoreFiles();

            var repoDirectoriesProvider = new RepoDirectoriesProvider();

            var nugetPackagesRoot = repoDirectoriesProvider.NugetPackages;

            var toolPathCalculator = new ToolPathCalculator(nugetPackagesRoot);

            var lockFilePath = toolPathCalculator.GetLockFilePath(
                "dotnet-portable",
                new NuGetVersion("1.0.0"),
                s_toolPackageFramework);

            var lockFile = new LockFileFormat().Read(lockFilePath);

            var depsJsonFile = Path.Combine(
                Path.GetDirectoryName(lockFilePath),
                "dotnet-portable.deps.json");

            if (File.Exists(depsJsonFile))
            {
                File.Delete(depsJsonFile);
            }
            File.WriteAllText(depsJsonFile, "temp");

            var projectToolsCommandResolver = SetupProjectToolsCommandResolver();
            projectToolsCommandResolver.GenerateDepsJsonFile(
                lockFile,
                s_toolPackageFramework,
                depsJsonFile,
                new SingleProjectInfo("dotnet-portable", "1.0.0", Enumerable.Empty<ResourceAssemblyInfo>()),
                GetToolDepsJsonGeneratorProject());

            File.ReadAllText(depsJsonFile).Should().Be("temp");
            File.Delete(depsJsonFile);
        }

        [Fact]
        public void ItAddsFxVersionAsAParamWhenTheToolHasThePrefercliruntimeFile()
        {
            var projectToolsCommandResolver = SetupProjectToolsCommandResolver();

            var testInstance = TestAssets.Get("MSBuildTestApp")
                .CreateInstance()
                .WithSourceFiles()
                .WithRestoreFiles();

            var commandResolverArguments = new CommandResolverArguments()
            {
                CommandName = "dotnet-prefercliruntime",
                CommandArguments = null,
                ProjectDirectory = testInstance.Root.FullName
            };

            var result = projectToolsCommandResolver.Resolve(commandResolverArguments);

            result.Should().NotBeNull();

            result.Args.Should().Contain("--fx-version 2.0.0");
        }

        [Fact]
        public void ItDoesNotAddFxVersionAsAParamWhenTheToolDoesNotHaveThePrefercliruntimeFile()
        {
            var projectToolsCommandResolver = SetupProjectToolsCommandResolver();

            var testInstance = TestAssets.Get(TestProjectName)
                .CreateInstance()
                .WithSourceFiles()
                .WithRestoreFiles();

            var commandResolverArguments = new CommandResolverArguments()
            {
                CommandName = "dotnet-portable",
                CommandArguments = null,
                ProjectDirectory = testInstance.Root.FullName
            };

            var result = projectToolsCommandResolver.Resolve(commandResolverArguments);

            result.Should().NotBeNull();

            result.Args.Should().NotContain("--fx-version");
        }

        [Fact]
        public void ItFindsToolsLocatedInTheNuGetFallbackFolder()
        {
            var projectToolsCommandResolver = SetupProjectToolsCommandResolver();

            var testInstance = TestAssets.Get("AppWithFallbackFolderToolDependency")
                .CreateInstance()
                .WithSourceFiles()
                .WithNuGetConfig(new RepoDirectoriesProvider().TestPackages);
            var testProjectDirectory = testInstance.Root.FullName;
            var fallbackFolder = Path.Combine(testProjectDirectory, "fallbackFolder");

            PopulateFallbackFolder(testProjectDirectory, fallbackFolder);

            var nugetConfig = UseNuGetConfigWithFallbackFolder(testInstance, fallbackFolder);

            new RestoreCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .Execute($"--configfile {nugetConfig}")
                .Should()
                .Pass();

            var commandResolverArguments = new CommandResolverArguments()
            {
                CommandName = "dotnet-fallbackfoldertool",
                CommandArguments = null,
                ProjectDirectory = testProjectDirectory
            };

            var result = projectToolsCommandResolver.Resolve(commandResolverArguments);

            result.Should().NotBeNull();

            var commandPath = result.Args.Trim('"');
            commandPath.Should().Contain(Path.Combine(
                fallbackFolder,
                "dotnet-fallbackfoldertool",
                "1.0.0",
                "lib",
                "netcoreapp2.0",
                "dotnet-fallbackfoldertool.dll"));
        }

        [Fact]
        public void ItShowsAnErrorWhenTheToolDllIsNotFound()
        {
            var projectToolsCommandResolver = SetupProjectToolsCommandResolver();

            var testInstance = TestAssets.Get("AppWithFallbackFolderToolDependency")
                .CreateInstance()
                .WithSourceFiles()
                .WithNuGetConfig(new RepoDirectoriesProvider().TestPackages);
            var testProjectDirectory = testInstance.Root.FullName;
            var fallbackFolder = Path.Combine(testProjectDirectory, "fallbackFolder");

            PopulateFallbackFolder(testProjectDirectory, fallbackFolder);

            var nugetConfig = UseNuGetConfigWithFallbackFolder(testInstance, fallbackFolder);

            new RestoreCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .Execute($"--configfile {nugetConfig}")
                .Should()
                .Pass();

            Directory.Delete(Path.Combine(fallbackFolder, "dotnet-fallbackfoldertool"), true);

            var commandResolverArguments = new CommandResolverArguments()
            {
                CommandName = "dotnet-fallbackfoldertool",
                CommandArguments = null,
                ProjectDirectory = testProjectDirectory
            };

            Action action = () => projectToolsCommandResolver.Resolve(commandResolverArguments);

            action.ShouldThrow<GracefulException>().WithMessage(
                string.Format(LocalizableStrings.CommandAssembliesNotFound, "dotnet-fallbackfoldertool"));
        }

        private void PopulateFallbackFolder(string testProjectDirectory, string fallbackFolder)
        {
            var nugetConfigPath = Path.Combine(testProjectDirectory, "NuGet.Config");
            new RestoreCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .Execute($"--configfile {nugetConfigPath} --packages {fallbackFolder}")
                .Should()
                .Pass();

            Directory.Delete(Path.Combine(fallbackFolder, ".tools"), true);
        }

        private string UseNuGetConfigWithFallbackFolder(TestAssetInstance testInstance, string fallbackFolder)
        {
            var nugetConfig = testInstance.Root.GetFile("NuGet.Config").FullName;
            File.WriteAllText(
                nugetConfig,
                $@"<?xml version=""1.0"" encoding=""utf-8""?>
                <configuration>
                  <fallbackPackageFolders>
                        <add key=""MachineWide"" value=""{fallbackFolder}""/>
                    </fallbackPackageFolders>
                </configuration>
                ");

            return nugetConfig;
        }

        private ProjectToolsCommandResolver SetupProjectToolsCommandResolver()
        {
            Environment.SetEnvironmentVariable(
                Constants.MSBUILD_EXE_PATH,
                Path.Combine(new RepoDirectoriesProvider().Stage2Sdk, "MSBuild.dll"));

            var packagedCommandSpecFactory = new PackagedCommandSpecFactoryWithCliRuntime();

            var projectToolsCommandResolver =
                new ProjectToolsCommandResolver(packagedCommandSpecFactory, new EnvironmentProvider());

            return projectToolsCommandResolver;
        }

        private string GetToolDepsJsonGeneratorProject()
        {
            //  When using the product, the ToolDepsJsonGeneratorProject property is used to get this path, but for testing
            //  we'll hard code the path inside the SDK since we don't have a project to evaluate here
            return Path.Combine(new RepoDirectoriesProvider().Stage2Sdk, "Sdks", "Microsoft.NET.Sdk", "build", "GenerateDeps", "GenerateDeps.proj");
        }
    }
}
