// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.Build.Construction;
using Microsoft.DotNet.Cli.Sln.Internal;
using Microsoft.DotNet.Tools.Test.Utilities;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.DotNet.Cli.Add.Proj.Tests
{
    public class GivenDotnetAddProj : TestBase
    {
        const string TestGroup = "NonRestoredTestProjects";
        const string ProjectName = "DotnetAddProjProjects";

        [Theory]
        [InlineData("--help")]
        [InlineData("-h")]
        public void WhenHelpOptionIsPassedItPrintsUsage(string helpArg)
        {
            var cmd = new AddProjCommand().Execute(helpArg);
            cmd.Should().Pass();
            cmd.StdOut.Should().Contain("Usage");
        }

        [Theory]
        [InlineData("idontexist.sln")]
        [InlineData("ihave?inv@lid/char\\acters")]
        public void WhenNonExistingSolutionIsPassedItPrintsErrorAndUsage(string solutionName)
        {
            var setup = CreateTestSetup(TestGroup, ProjectName);

            var cmd = new AddProjCommand()
                .WithWorkingDirectory(setup.TestRoot)
                .WithSolution(solutionName)
                .Execute($"\"{setup.GetProjectFullPath("Lib")}\"");
            cmd.ExitCode.Should().NotBe(0);
            cmd.StdErr.Should().Contain("Could not find");
            cmd.StdOut.Should().Contain("Usage");
        }

        [Theory]
        [InlineData("Invalid.sln")]
        [InlineData("")]
        public void WhenInvalidSolutionIsPassedItPrintsErrorAndUsage(string slnName)
        {
            var setup = CreateTestSetup(TestGroup, ProjectName);

            var cmd = new AddProjCommand()
                .WithWorkingDirectory(setup.TestRoot)
                .WithSolution(slnName)
                .Execute($"\"{setup.GetProjectFullPath("Lib")}\"");
            cmd.ExitCode.Should().NotBe(0);
            cmd.StdErr.Should().Contain($"Invalid solution ");
            cmd.StdOut.Should().Contain("Usage");
        }

        [Fact]
        public void WhenNoProjectIsPassedItPrintsErrorAndUsage()
        {
            var setup = CreateTestSetup(TestGroup, ProjectName);

            var cmd = new AddProjCommand()
                .WithWorkingDirectory(Path.Combine(setup.TestRoot, "MoreThanOne"))
                .WithSolution("App.sln")
                .Execute();
            cmd.ExitCode.Should().NotBe(0);
            cmd.StdErr.Should().Contain("You must specify at least one project to add.");
            cmd.StdOut.Should().Contain("Usage");
        }

        [Fact]
        public void WhenMoreThanOneSolutionExistsInTheDirectoryItPrintsErrorAndUsage()
        {
            var setup = CreateTestSetup(TestGroup, ProjectName);

            var cmd = new AddProjCommand()
                .WithWorkingDirectory(Path.Combine(setup.TestRoot, "MoreThanOne"))
                .Execute($"\"{setup.GetProjectFullPath("Lib")}\"");
            cmd.ExitCode.Should().NotBe(0);
            cmd.StdErr.Should().Contain("more than one");
            cmd.StdOut.Should().Contain("Usage");
        }

        [Fact]
        public void WhenNoSolutionExistsInTheDirectoryItPrintsErrorAndUsage()
        {
            var setup = CreateTestSetup(TestGroup, ProjectName);

            var cmd = new AddProjCommand()
                .WithWorkingDirectory(Path.Combine(setup.TestRoot, "App"))
                .Execute($"\"{setup.GetProjectFullPath("Lib")}\"");
            cmd.ExitCode.Should().NotBe(0);
            cmd.StdErr.Should().Contain("does not exist");
            cmd.StdOut.Should().Contain("Usage");
        }

        [Theory]
        [InlineData("Lib", "", false)]
        [InlineData("LibWithProjectGuid", "{84A45D44-B677-492D-A6DA-B3A71135AB8E}", false)]
        [InlineData("Lib", "", true)]
        [InlineData("LibWithProjectGuid", "{84A45D44-B677-492D-A6DA-B3A71135AB8E}", true)]
        public void ItAddsProjectAndPrintsStatus(string projectName, string projectGuid, bool makeRelative)
        {
            var setup = CreateTestSetup(TestGroup, ProjectName);
            string slnDir = Path.Combine(setup.TestRoot, "MoreThanOne");
            string slnName = "App.sln";

            var projectToAdd = makeRelative
                ? $"\"..{Path.DirectorySeparatorChar}{projectName}{Path.DirectorySeparatorChar}{projectName}.csproj\""
                : $"\"{setup.GetProjectFullPath(projectName)}\"";

            var cmd = new AddProjCommand()
                .WithWorkingDirectory(slnDir)
                .WithSolution(slnName)
                .Execute(projectToAdd);
            cmd.Should().Pass();
            cmd.StdOut.Should().Contain("added to the solution");
            cmd.StdErr.Should().BeEmpty();

            //var slnFile = new SlnFile();
            //slnFile.Read(Path.Combine(slnDir, slnName));
            //VerifyProjectInSolution(slnFile, projectName, projectGuid);
            var contentBefore = File.ReadAllText(Path.Combine(slnDir, slnName));
            Restore(Path.Combine(setup.TestRoot, "App"), "App.csproj");
            Build(slnDir, slnName);
            contentBefore.Should().Be("I'm debugging");
        }

        [Fact]
        public void WhenSolutionAlreadyContainsProjectItDoesntDuplicate()
        {
            var setup = CreateTestSetup(TestGroup, ProjectName);
            string slnDir = Path.Combine(setup.TestRoot, "MoreThanOne");
            string slnName = "AppAndLib.sln";
            string slnFullPath = Path.Combine(slnDir, slnName);

            var contentBefore = File.ReadAllText(slnFullPath);
            var cmd = new AddProjCommand()
                .WithWorkingDirectory(slnDir)
                .WithSolution(slnName)
                .Execute($"\"{setup.GetProjectFullPath("Lib")}\"");
            cmd.Should().Pass();
            cmd.StdOut.Should().Contain("already contains project");
            cmd.StdErr.Should().BeEmpty();

            var contentAfter = File.ReadAllText(slnFullPath);
            contentAfter.Should().BeEquivalentTo(contentBefore);
        }

        [Fact]
        public void WhenPassedMultipleProjectsAndOneOfthemDoesNotExistItCancelsWholeOperation()
        {
            var setup = CreateTestSetup(TestGroup, ProjectName);
            string slnDir = Path.Combine(setup.TestRoot, "MoreThanOne");
            string slnName = "App.sln";
            string slnFullPath = Path.Combine(slnDir, slnName);

            var contentBefore = File.ReadAllText(slnFullPath);
            var cmd = new AddProjCommand()
                .WithWorkingDirectory(slnDir)
                .WithSolution(slnName)
                .Execute($"\"{setup.GetProjectFullPath("Lib")}\" \"IDoNotExist.csproj\"");
            cmd.Should().Fail();
            cmd.StdErr.Should().Contain("does not exist");
            cmd.StdErr.Should().NotMatchRegex("(.*does not exist.*){2,}");

            var contentAfter = File.ReadAllText(slnFullPath);
            contentAfter.Should().BeEquivalentTo(contentBefore);
        }

        [Fact]
        public void WhenPassedProjectDoesNotExistAndForceSwitchIsPassedItAddsIt()
        {
            var setup = CreateTestSetup(TestGroup, ProjectName);
            string slnDir = Path.Combine(setup.TestRoot, "MoreThanOne");
            string slnName = "App.sln";
            string slnFullPath = Path.Combine(slnDir, slnName);

            var contentBefore = File.ReadAllText(slnFullPath);
            var cmd = new AddProjCommand()
                .WithWorkingDirectory(slnDir)
                .WithSolution(slnName)
                .Execute("--force \"IDoNotExist.csproj\"");
            cmd.Should().Pass();
            cmd.StdOut.Should().Contain("added to the solution");
            cmd.StdErr.Should().BeEmpty();
        }

        private void VerifyProjectInSolution(SlnFile slnFile, string projectName, string projectGuid)
        {
            var projects = slnFile.Projects
                .Where((p) => p.Name == projectName)
                .ToList();

            projects.Count.Should().Be(1);
            if (!string.IsNullOrEmpty(projectGuid))
            {
                projects[0].Id.Should().Be(projectGuid);
            }
            projects[0].TypeGuid.Should().Be(SlnFile.CSharpProjectTypeGuid);
            projects[0].FilePath.Should().Be($"..\\{projectName}\\{projectName}.csproj");
        }

        private void Restore(string projectDirectory, string projectName)
        {
            var command = new RestoreCommand()
                .WithWorkingDirectory(projectDirectory);

            if (!Path.HasExtension(projectName))
            {
                projectName += ".csproj";
            }

            command.Execute($"{projectName}")
                .Should().Pass();
        }

        private void Build(string solutionDirectory, string solutionName, string configuration = "Debug")
        {
            if (!Path.HasExtension(solutionName))
            {
                solutionName += ".sln";
            }

            var result = new BuildCommand()
                .WithWorkingDirectory(solutionDirectory)
                .ExecuteWithCapturedOutput($"{solutionName} /p:Configuration={configuration}");

            result.Should().Pass();
        }
    }
}
