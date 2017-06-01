// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using FluentAssertions;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;

namespace Microsoft.DotNet.Cli.Run.Tests
{
    public class GivenDotnetRunBuildsCsproj : TestBase
    {
        [Fact]
        public void ItCanRunAMSBuildProject()
        {
            var testAppName = "MSBuildTestApp";
            var testInstance = TestAssets.Get(testAppName)
                            .CreateInstance()
                            .WithSourceFiles();

            var testProjectDirectory = testInstance.Root.FullName;

            new RestoreCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .Execute("/p:SkipInvalidConfigurations=true")
                .Should().Pass();

            new BuildCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .Execute()
                .Should().Pass();

            new RunCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .ExecuteWithCapturedOutput()
                .Should().Pass()
                         .And.HaveStdOutContaining("Hello World!");
        }

        [Fact]
        public void ItBuildsTheProjectBeforeRunning()
        {
            var testAppName = "MSBuildTestApp";
            var testInstance = TestAssets.Get(testAppName)
                            .CreateInstance()
                            .WithSourceFiles();

            var testProjectDirectory = testInstance.Root.FullName;

            new RestoreCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .Execute("/p:SkipInvalidConfigurations=true")
                .Should().Pass();

            new RunCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .ExecuteWithCapturedOutput()
                .Should().Pass()
                         .And.HaveStdOutContaining("Hello World!");
        }

        [Fact]
        public void ItCanRunAMSBuildProjectWhenSpecifyingAFramework()
        {
            var testAppName = "MSBuildTestApp";
            var testInstance = TestAssets.Get(testAppName)
                            .CreateInstance()
                            .WithSourceFiles();

            var testProjectDirectory = testInstance.Root.FullName;

            new RestoreCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .Execute("/p:SkipInvalidConfigurations=true")
                .Should().Pass();

            new RunCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .ExecuteWithCapturedOutput("--framework netcoreapp2.0")
                .Should().Pass()
                         .And.HaveStdOutContaining("Hello World!");
        }

        [Fact]
        public void ItRunsPortableAppsFromADifferentPathAfterBuilding()
        {
            var testInstance = TestAssets.Get("MSBuildTestApp")
                .CreateInstance()
                .WithSourceFiles()
                .WithRestoreFiles();

            new BuildCommand()
                .WithWorkingDirectory(testInstance.Root)
                .Execute()
                .Should().Pass();

            new RunCommand()
                .WithWorkingDirectory(testInstance.Root)
                .ExecuteWithCapturedOutput($"--no-build")
                .Should().Pass()
                         .And.HaveStdOutContaining("Hello World!");
        }

        [Fact]
        public void ItRunsPortableAppsFromADifferentPathWithoutBuilding()
        {
            var testAppName = "MSBuildTestApp";
            var testInstance = TestAssets.Get(testAppName)
                .CreateInstance()
                .WithSourceFiles()
                .WithRestoreFiles();

            var projectFile = testInstance.Root.GetFile(testAppName + ".csproj");

            new RunCommand()
                .WithWorkingDirectory(testInstance.Root.Parent)
                .ExecuteWithCapturedOutput($"--project {projectFile.FullName}")
                .Should().Pass()
                         .And.HaveStdOutContaining("Hello World!");
        }

        [Fact]
        public void ItRunsPortableAppsFromADifferentPathSpecifyingOnlyTheDirectoryWithoutBuilding()
        {
            var testAppName = "MSBuildTestApp";
            var testInstance = TestAssets.Get(testAppName)
                .CreateInstance()
                .WithSourceFiles()
                .WithRestoreFiles();

            var testProjectDirectory = testInstance.Root.FullName;

            new RunCommand()
                .WithWorkingDirectory(testInstance.Root.Parent)
                .ExecuteWithCapturedOutput($"--project {testProjectDirectory}")
                .Should().Pass()
                         .And.HaveStdOutContaining("Hello World!");
        }

        [Fact]
        public void ItRunsAppWhenRestoringToSpecificPackageDirectory()
        {
            var rootPath = TestAssets.CreateTestDirectory().FullName;

            string dir = "pkgs";
            string args = $"--packages {dir}";

            string newArgs = $"console -o \"{rootPath}\" --no-restore";
            new NewCommandShim()
                .WithWorkingDirectory(rootPath)
                .Execute(newArgs)
                .Should()
                .Pass();

            new RestoreCommand()
                .WithWorkingDirectory(rootPath)
                .Execute(args)
                .Should()
                .Pass();

            new RunCommand()
                .WithWorkingDirectory(rootPath)
                .ExecuteWithCapturedOutput()
                .Should().Pass()
                         .And.HaveStdOutContaining("Hello World");
        }

        [Fact]
        public void ItReportsAGoodErrorWhenProjectHasMultipleFrameworks()
        {
            var testAppName = "MSBuildAppWithMultipleFrameworks";
            var testInstance = TestAssets.Get(testAppName)
                .CreateInstance()
                .WithSourceFiles()
                .WithRestoreFiles();

            // use --no-build so this test can run on all platforms.
            // the test app targets net451, which can't be built on non-Windows
            new RunCommand()
                .WithWorkingDirectory(testInstance.Root)
                .ExecuteWithCapturedOutput("--no-build")
                .Should().Fail()
                    .And.HaveStdErrContaining("--framework");
        }

        [Fact]
        public void ItCanPassArgumentsToSubjectAppByDoubleDash()
        {
            const string testAppName = "MSBuildTestApp";
            var testInstance = TestAssets.Get(testAppName)
                .CreateInstance()
                .WithSourceFiles()
                .WithRestoreFiles();

            var testProjectDirectory = testInstance.Root.FullName;

            new RunCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .ExecuteWithCapturedOutput("-- foo bar baz")
                .Should()
                .Pass()
                .And.HaveStdOutContaining("echo args:foo;bar;baz");
        }

        [Fact]
        public void ItGivesAnErrorWhenAttemptingToUseALaunchProfileThatDoesNotExistWhenThereIsNoLaunchSettingsFile()
        {
            var testAppName = "MSBuildTestApp";
            var testInstance = TestAssets.Get(testAppName)
                            .CreateInstance()
                            .WithSourceFiles();

            var testProjectDirectory = testInstance.Root.FullName;

            new RestoreCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .Execute("/p:SkipInvalidConfigurations=true")
                .Should().Pass();

            new BuildCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .Execute()
                .Should().Pass();

            new RunCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .ExecuteWithCapturedOutput("--launch-profile test")
                .Should().Pass()
                         .And.HaveStdOutContaining("Hello World!")
                         .And.HaveStdErrContaining("The specified launch profile could not be located.");
        }

        [Fact]
        public void ItUsesLaunchProfileOfTheSpecifiedName()
        {
            var testAppName = "AppWithLaunchSettings";
            var testInstance = TestAssets.Get(testAppName)
                            .CreateInstance()
                            .WithSourceFiles();

            var testProjectDirectory = testInstance.Root.FullName;

            new RestoreCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .Execute("/p:SkipInvalidConfigurations=true")
                .Should().Pass();

            new BuildCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .Execute()
                .Should().Pass();

            var cmd = new RunCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .ExecuteWithCapturedOutput("--launch-profile Second");

            cmd.Should().Pass()
                .And.HaveStdOutContaining("Second");

            cmd.StdErr.Should().BeEmpty();
        }

        [Fact]
        public void ItDefaultsToTheFirstUsableLaunchProfile()
        {
            var testAppName = "AppWithLaunchSettings";
            var testInstance = TestAssets.Get(testAppName)
                            .CreateInstance()
                            .WithSourceFiles();

            var testProjectDirectory = testInstance.Root.FullName;

            new RestoreCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .Execute("/p:SkipInvalidConfigurations=true")
                .Should().Pass();

            new BuildCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .Execute()
                .Should().Pass();

            var cmd = new RunCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .ExecuteWithCapturedOutput();

            cmd.Should().Pass()
                .And.HaveStdOutContaining("First");
                         
            cmd.StdErr.Should().BeEmpty();
        }

        [Fact]
        public void ItGivesAnErrorWhenTheLaunchProfileNotFound()
        {
            var testAppName = "AppWithLaunchSettings";
            var testInstance = TestAssets.Get(testAppName)
                            .CreateInstance()
                            .WithSourceFiles();

            var testProjectDirectory = testInstance.Root.FullName;

            new RestoreCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .Execute("/p:SkipInvalidConfigurations=true")
                .Should().Pass();

            new BuildCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .Execute()
                .Should().Pass();

            new RunCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .ExecuteWithCapturedOutput("--launch-profile Third")
                .Should().Pass()
                         .And.HaveStdOutContaining("(NO MESSAGE)")
                         .And.HaveStdErrContaining("The launch profile \"Third\" could not be applied.");
        }

        [Fact]
        public void ItGivesAnErrorWhenTheLaunchProfileCanNotBeHandled()
        {
            var testAppName = "AppWithLaunchSettings";
            var testInstance = TestAssets.Get(testAppName)
                            .CreateInstance()
                            .WithSourceFiles();

            var testProjectDirectory = testInstance.Root.FullName;

            new RestoreCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .Execute("/p:SkipInvalidConfigurations=true")
                .Should().Pass();

            new BuildCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .Execute()
                .Should().Pass();

            new RunCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .ExecuteWithCapturedOutput("--launch-profile \"IIS Express\"")
                .Should().Pass()
                         .And.HaveStdOutContaining("(NO MESSAGE)")
                         .And.HaveStdErrContaining("The launch profile \"IIS Express\" could not be applied.");
        }

        [Fact]
        public void ItSkipsLaunchProfilesWhenTheSwitchIsSupplied()
        {
            var testAppName = "AppWithLaunchSettings";
            var testInstance = TestAssets.Get(testAppName)
                            .CreateInstance()
                            .WithSourceFiles();

            var testProjectDirectory = testInstance.Root.FullName;

            new RestoreCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .Execute("/p:SkipInvalidConfigurations=true")
                .Should().Pass();

            new BuildCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .Execute()
                .Should().Pass();

            var cmd = new RunCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .ExecuteWithCapturedOutput("--no-launch-profile");
                
            cmd.Should().Pass()
                .And.HaveStdOutContaining("(NO MESSAGE)");

            cmd.StdErr.Should().BeEmpty();
        }

        [Fact]
        public void ItSkipsLaunchProfilesWhenTheSwitchIsSuppliedWithoutErrorWhenThereAreNoLaunchSettings()
        {
            var testAppName = "MSBuildTestApp";
            var testInstance = TestAssets.Get(testAppName)
                            .CreateInstance()
                            .WithSourceFiles();

            var testProjectDirectory = testInstance.Root.FullName;

            new RestoreCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .Execute("/p:SkipInvalidConfigurations=true")
                .Should().Pass();

            new BuildCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .Execute()
                .Should().Pass();

            var cmd = new RunCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .ExecuteWithCapturedOutput("--no-launch-profile");

            cmd.Should().Pass()
                .And.HaveStdOutContaining("Hello World!");

            cmd.StdErr.Should().BeEmpty();
        }

        [Fact]
        public void ItSkipsLaunchProfilesWhenThereIsNoUsableDefault()
        {
            var testAppName = "AppWithLaunchSettingsNoDefault";
            var testInstance = TestAssets.Get(testAppName)
                            .CreateInstance()
                            .WithSourceFiles();

            var testProjectDirectory = testInstance.Root.FullName;

            new RestoreCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .Execute("/p:SkipInvalidConfigurations=true")
                .Should().Pass();

            new BuildCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .Execute()
                .Should().Pass();

            var cmd = new RunCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .ExecuteWithCapturedOutput();

            cmd.Should().Pass()
                .And.HaveStdOutContaining("(NO MESSAGE)")
                .And.HaveStdErrContaining("The launch profile \"(Default)\" could not be applied.");
        }

        [Fact]
        public void ItPrintsAnErrorWhenLaunchSettingsAreCorrupted()
        {
            var testAppName = "AppWithCorruptedLaunchSettings";
            var testInstance = TestAssets.Get(testAppName)
                            .CreateInstance()
                            .WithSourceFiles();

            var testProjectDirectory = testInstance.Root.FullName;

            new RestoreCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .Execute("/p:SkipInvalidConfigurations=true")
                .Should().Pass();

            new BuildCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .Execute()
                .Should().Pass();

            var cmd = new RunCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .ExecuteWithCapturedOutput();

            cmd.Should().Pass()
                .And.HaveStdOutContaining("(NO MESSAGE)")
                .And.HaveStdErrContaining("The launch profile \"(Default)\" could not be applied.");
        }
    }
}
