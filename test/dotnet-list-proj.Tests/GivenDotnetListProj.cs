// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.Cli.Sln.Internal;
using Microsoft.DotNet.Tools.Test.Utilities;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.DotNet.Cli.List.Proj.Tests
{
    public class GivenDotnetListProj : TestBase
    {
        [Theory]
        [InlineData("--help")]
        [InlineData("-h")]
        public void WhenHelpOptionIsPassedItPrintsUsage(string helpArg)
        {
            var cmd = new DotnetCommand()
                .ExecuteWithCapturedOutput($"list projects {helpArg}");
            cmd.Should().Pass();
            cmd.StdOut.Should().Contain("Usage");
        }

        [Fact]
        public void WhenTooManyArgumentsArePassedItPrintsError()
        {
            var cmd = new DotnetCommand()
                .ExecuteWithCapturedOutput("list one.sln two.sln three.sln projects");
            cmd.Should().Fail();
            cmd.StdErr.Should().Contain("Unrecognized command or argument");
        }

        [Theory]
        [InlineData("idontexist.sln")]
        [InlineData("ihave?invalidcharacters.sln")]
        [InlineData("ihaveinv@lidcharacters.sln")]
        [InlineData("ihaveinvalid/characters")]
        [InlineData("ihaveinvalidchar\\acters")]
        public void WhenNonExistingSolutionIsPassedItPrintsErrorAndUsage(string solutionName)
        {
            var cmd = new DotnetCommand()
                .ExecuteWithCapturedOutput($"list {solutionName} projects");
            cmd.Should().Fail();
            cmd.StdErr.Should().Contain("Could not find");
            cmd.StdOut.Should().Contain("Usage:");
        }

        [Fact]
        public void WhenInvalidSolutionIsPassedItPrintsErrorAndUsage()
        {
            var projectDirectory = TestAssetsManager.CreateTestInstance("InvalidSolution")
                                                    .WithLockFiles()
                                                    .Path;

            var cmd = new DotnetCommand()
                .WithWorkingDirectory(projectDirectory)
                .ExecuteWithCapturedOutput("list InvalidSolution.sln projects");
            cmd.Should().Fail();
            cmd.StdErr.Should().Contain("Invalid solution ");
            cmd.StdOut.Should().Contain("Usage:");
        }

        [Fact]
        public void WhenInvalidSolutionIsFoundItPrintsErrorAndUsage()
        {
            var projectDirectory = TestAssetsManager.CreateTestInstance("InvalidSolution")
                                                    .WithLockFiles()
                                                    .Path;

            var cmd = new DotnetCommand()
                .WithWorkingDirectory(projectDirectory)
                .ExecuteWithCapturedOutput("list projects");
            cmd.Should().Fail();
            cmd.StdErr.Should().Contain("Invalid solution ");
            cmd.StdOut.Should().Contain("Usage:");
        }

        [Fact]
        public void WhenNoSolutionExistsInTheDirectoryItPrintsErrorAndUsage()
        {
            var projectDirectory = TestAssetsManager.CreateTestInstance("TestAppWithSlnAndCsprojFiles")
                                                    .WithLockFiles()
                                                    .Path;

            var cmd = new DotnetCommand()
                .WithWorkingDirectory(Path.Combine(projectDirectory, "App"))
                .ExecuteWithCapturedOutput("list projects");
            cmd.Should().Fail();
            cmd.StdErr.Should().Contain("does not exist");
            cmd.StdOut.Should().Contain("Usage:");
        }

        [Fact]
        public void WhenMoreThanOneSolutionExistsInTheDirectoryItPrintsErrorAndUsage()
        {
            var projectDirectory = TestAssetsManager.CreateTestInstance("TestAppWithMultipleSlnFiles")
                                                    .WithLockFiles()
                                                    .Path;

            var cmd = new DotnetCommand()
                .WithWorkingDirectory(projectDirectory)
                .ExecuteWithCapturedOutput("list projects");
            cmd.Should().Fail();
            cmd.StdErr.Should().Contain("more than one");
            cmd.StdOut.Should().Contain("Usage");
        }

        [Fact]
        public void WhenNoProjectReferencesArePresentInTheSolutionItPrintsError()
        {
            var projectDirectory = TestAssetsManager.CreateTestInstance("SlnFileWithNoProjectReferences")
                                                    .WithLockFiles()
                                                    .Path;

            var cmd = new DotnetCommand()
                .WithWorkingDirectory(projectDirectory)
                .ExecuteWithCapturedOutput("list projects");
            cmd.Should().Pass();
            cmd.StdOut.Should().Contain("No projects found in the solution.");
        }

        [Fact]
        public void WhenProjectReferencesArePresentInTheSolutionItListsThem()
        {
            var projectDirectory = TestAssetsManager.CreateTestInstance("TestAppWithSlnAndExistingCsprojReferences")
                                                    .WithLockFiles()
                                                    .Path;

            var cmd = new DotnetCommand()
                .WithWorkingDirectory(projectDirectory)
                .ExecuteWithCapturedOutput("list projects");
            cmd.Should().Pass();
            cmd.StdOut.Should().Contain("Project reference(s)");
            cmd.StdOut.Should().Contain(@"App\App.csproj");
            cmd.StdOut.Should().Contain(@"Lib\Lib.csproj");
        }
    }
}
