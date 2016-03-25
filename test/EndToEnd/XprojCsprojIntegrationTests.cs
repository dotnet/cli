// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;

namespace Microsoft.DotNet.Tests.EndToEnd
{
    public class XprojCsprojIntegrationTests : TestBase
    {
        private static string XprojCsprojProjects => Path.Combine(RepoRoot, "TestAssets", "AsIsProjects", "XprojCsprojProjects");

        [Fact]
        public void BasicSolutionBuilds()
        {
            var testProjectDirectory = CreateTestProjectFrom("BasicProject_valid");
            var projectRoot = Path.Combine(testProjectDirectory.Path, "Sample1-Xproj+Csproj", "src", "ConsoleApp13");

            BuildProject("ConsoleApp13", projectRoot, expectIncremental: false, noDependencies: true);
            BuildProject("ConsoleApp13", projectRoot, expectIncremental: true, noDependencies: true);

            // touch the fragment file to trigger a rebuild
            var fragmentPath = Path.Combine(projectRoot, "project.fragment.lock.json");
            File.SetLastWriteTimeUtc(fragmentPath, DateTime.UtcNow);

            BuildProject("ConsoleApp13", projectRoot, expectIncremental: false, noDependencies: true);
        }

        [Fact]
        public void BasicSolutionWithMissingExportDllFails()
        {
            var testProjectDirectory = CreateTestProjectFrom("BasicProject_invalid-missing-dll");
            var projectRoot = Path.Combine(testProjectDirectory.Path, "Sample1-Xproj+Csproj", "src", "ConsoleApp13");

            BuildProject("ConsoleApp13", projectRoot, noDependencies: true, expectFailure: true);
        }

        private CommandResult BuildProject(string projectName, string projectFile, bool expectIncremental = false, bool noDependencies = false, bool expectFailure = false)
        {
            var buildCommand = new BuildCommand(projectFile, noDependencies: noDependencies);
            var result = buildCommand.ExecuteWithCapturedOutput();

            if (expectFailure)
            {
                result.Should().Fail();
            }
            else
            {
                result.Should().Pass();
            }

            if (expectIncremental)
            {
                result.Should().HaveSkippedProjectCompilation(projectName);
            }
            else
            {
                result.Should().HaveCompiledProject(projectName);
            }

            return result;
        }

        private TempDirectory CreateTestProjectFrom(string projectName)
        {
            var dir = Temp.CreateDirectory();
            var projectPath = GetProjectPath(projectName);
            return dir.CopyDirectory(projectPath);
        }

        private static string GetProjectPath(string projectName)
        {
            return Path.Combine(XprojCsprojProjects, projectName);
        }
    }

}
