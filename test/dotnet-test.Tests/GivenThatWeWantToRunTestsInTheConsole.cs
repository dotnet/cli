﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.TestFramework;
using Microsoft.Extensions.PlatformAbstractions;
using Xunit;
using Microsoft.DotNet.Tools.Test.Utilities;
using System.Linq;

namespace Microsoft.Dotnet.Tools.Test.Tests
{
    public class GivenThatWeWantToRunTestsInTheConsole : TestBase
    {
        private string _projectFilePath;
        private string _defaultOutputPath;

        public GivenThatWeWantToRunTestsInTheConsole()
        {
            var testAssetManager = new TestAssetsManager(Path.Combine(RepoRoot, "TestAssets"));
            var testInstance =
                testAssetManager.CreateTestInstance("ProjectWithTests", identifier: "ConsoleTests");

            _projectFilePath = Path.Combine(testInstance.TestRoot, "project.json");
            var contexts = ProjectContext.CreateContextForEachFramework(
                _projectFilePath,
                null,
                PlatformServices.Default.Runtime.GetAllCandidateRuntimeIdentifiers());

            // Restore the project again in the destination to resolve projects
            // Since the lock file has project relative paths in it, those will be broken
            // unless we re-restore
            new RestoreCommand() { WorkingDirectory = testInstance.TestRoot }.Execute().Should().Pass();

            _defaultOutputPath = Path.Combine(testInstance.TestRoot, "bin", "Debug", "netcoreapp1.0");
        }

        //ISSUE https://github.com/dotnet/cli/issues/1935
        // This fact technically succeeds on Windows, but it causes a crash dialog to pop, which interrupts the build.
        //[WindowsOnlyFact]
        public void It_returns_a_failure_when_it_fails_to_run_the_tests()
        {
            var testCommand = new DotnetTestCommand();
            var result = testCommand.Execute(
                $"{_projectFilePath} -o {Path.Combine(AppContext.BaseDirectory, "nonExistingFolder")} --no-build");
            result.Should().Fail();
        }

        [Fact]
        public void It_builds_the_project_before_running()
        {
            var testCommand = new DotnetTestCommand();
            var result = testCommand.Execute($"{_projectFilePath}");
            result.Should().Pass();
        }

        [Fact]
        public void It_builds_the_project_using_the_output_passed()
        {
            var testCommand = new DotnetTestCommand();
            var result = testCommand.Execute(
                $"{_projectFilePath} -o {Path.Combine(AppContext.BaseDirectory, "output")} -f netcoreapp1.0");
            result.Should().Pass();
        }

        [Fact]
        public void It_builds_the_project_using_the_build_base_path_passed()
        {
            var buildBasePath = GetNotSoLongBuildBasePath();
            var testCommand = new DotnetTestCommand();
            var result = testCommand.Execute($"{_projectFilePath} -b {buildBasePath}");
            result.Should().Pass();
        }

        [Fact]
        public void It_skips_build_when_the_no_build_flag_is_passed()
        {
            var buildCommand = new BuildCommand(_projectFilePath);
            var result = buildCommand.Execute();
            result.Should().Pass();

            var testCommand = new DotnetTestCommand();
            result = testCommand.Execute($"{_projectFilePath} -o {_defaultOutputPath} --no-build");
            result.Should().Pass();
        }

        private string GetNotSoLongBuildBasePath()
        {
            return Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "buildBasePathTest"));
        }
    }
}
