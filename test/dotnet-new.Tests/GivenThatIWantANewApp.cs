﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using FluentAssertions;
using Microsoft.DotNet.Tools.Test.Utilities;
using Microsoft.Extensions.DependencyModel;
using Xunit;

namespace Microsoft.DotNet.New.Tests
{
    public class GivenThatIWantANewApp : TestBase
    {
        [Fact]
        public void When_dotnet_new_is_invoked_mupliple_times_it_should_fail()
        {
            var rootPath = TestAssets.CreateTestDirectory().FullName;

            new NewCommand()
                .WithWorkingDirectory(rootPath)
                .Execute($"console --debug:ephemeral-hive --no-restore");

            DateTime expectedState = Directory.GetLastWriteTime(rootPath);

            var result = new NewCommand()
                .WithWorkingDirectory(rootPath)
                .ExecuteWithCapturedOutput($"console --debug:ephemeral-hive --no-restore");

            DateTime actualState = Directory.GetLastWriteTime(rootPath);

            Assert.Equal(expectedState, actualState);

            result.Should().Fail();
        }

        [Fact]
        public void RestoreDoesNotUseAnyCliProducedPackagesOnItsTemplates()
        {
            string[] cSharpTemplates = new[] { "console", "classlib", "mstest", "xunit", "web", "mvc", "webapi" };

            var rootPath = TestAssets.CreateTestDirectory().FullName;
            var packagesDirectory = Path.Combine(rootPath, "packages");

            var configFile = Path.Combine(RepoDirectoriesProvider.RepoRoot, "NuGet.Config");

            foreach (string cSharpTemplate in cSharpTemplates)
            {
                var projectFolder = Path.Combine(rootPath, cSharpTemplate + "1");
                Directory.CreateDirectory(projectFolder);
                CreateAndRestoreNewProject(cSharpTemplate, projectFolder, packagesDirectory, configFile);
            }

            Directory.EnumerateFiles(packagesDirectory, $"*.nupkg", SearchOption.AllDirectories)
                .Should().NotContain(p => p.Contains("Microsoft.DotNet.Cli.Utils"));
        }

        private void CreateAndRestoreNewProject(
            string projectType,
            string projectFolder,
            string packagesDirectory,
            string configFile)
        {
            new NewCommand()
                .WithWorkingDirectory(projectFolder)
                .Execute($"{projectType} --debug:ephemeral-hive --no-restore")
                .Should().Pass();

            new RestoreCommand()
                .WithWorkingDirectory(projectFolder)
                .Execute($"--configfile {configFile} --packages {packagesDirectory}")
                .Should().Pass();
        }

        // Remove the expectedVersion parameter once we have templates targetting netcoreapp2.2.
        [Theory]
        [InlineData("console", "microsoft.netcore.app", "2.1.0")]
        [InlineData("classlib", "netstandard.library", "2.0.3")] // FIXME: This is pinned to 2.0.3 due to https://github.com/dotnet/sdk/issues/2410
        public void NewProjectRestoresCorrectPackageVersion(string type, string packageName, string expectedVersion)
        {
            var rootPath = TestAssets.CreateTestDirectory(identifier: $"_{type}").FullName;
            var packagesDirectory = Path.Combine(rootPath, "packages");
            var projectName = "Project";
            expectedVersion = expectedVersion ?? GetFrameworkPackageVersion();

            var repoRootNuGetConfig = Path.Combine(RepoDirectoriesProvider.RepoRoot, "NuGet.Config");

            new NewCommand()
                .WithWorkingDirectory(rootPath)
                .Execute($"{type} --name {projectName} -o . --debug:ephemeral-hive --no-restore")
                .Should().Pass();

            new RestoreCommand()
                .WithWorkingDirectory(rootPath)
                .Execute($"--configfile {repoRootNuGetConfig} --packages {packagesDirectory}")
                .Should().Pass();

            new DirectoryInfo(Path.Combine(packagesDirectory, packageName))
                .Should().Exist()
                .And.HaveDirectory(expectedVersion);

            string GetFrameworkPackageVersion()
            {
                var dotnetDir = new FileInfo(DotnetUnderTest.FullName).Directory;
                var sharedFxDir = dotnetDir
                    .GetDirectory("shared", "Microsoft.NETCore.App")
                    .EnumerateDirectories()
                    .Single(d => d.Name.StartsWith("3.0.0"));

                if (packageName == "microsoft.netcore.app")
                {
                    return sharedFxDir.Name;
                }

                var depsFile = Path.Combine(sharedFxDir.FullName, "Microsoft.NETCore.App.deps.json");
                using (var stream = File.OpenRead(depsFile))
                using (var reader = new DependencyContextJsonReader())
                {
                    var context = reader.Read(stream);
                    var dependency = context
                        .RuntimeLibraries
                        .Single(library => string.Equals(library.Name, packageName, StringComparison.OrdinalIgnoreCase));

                    return dependency.Version;
                }
            }
        }
    }
}
