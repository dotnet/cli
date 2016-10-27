// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
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
            var testInstance = TestAssetsManager
                .CreateTestInstance(testAppName);

            var testProjectDirectory = testInstance.TestRoot;

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
            var testInstance = TestAssetsManager
                .CreateTestInstance(testAppName);

            var testProjectDirectory = testInstance.TestRoot;

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
            var testInstance = TestAssetsManager
                .CreateTestInstance(testAppName);

            var testProjectDirectory = testInstance.TestRoot;

            new RestoreCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .Execute("/p:SkipInvalidConfigurations=true")
                .Should().Pass();

            new RunCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .ExecuteWithCapturedOutput("--framework netcoreapp1.0")
                .Should().Pass()
                         .And.HaveStdOutContaining("Hello World!");            
        }
 
        [Fact] 
        public void It_runs_portable_apps_from_a_different_path_after_building() 
        { 
            var testAppName = "MSBuildTestApp"; 
            var testInstance = TestAssets.Get(testAppName)
                .CreateInstance()
                .WithSourceFiles(); 
 
            var testProjectDirectory = testInstance.TestRoot; 
 
            new RestoreCommand() 
                .WithWorkingDirectory(testProjectDirectory) 
                .Execute() 
                .Should().Pass(); 
 
            new BuildCommand() 
                .WithWorkingDirectory(testProjectDirectory) 
                .Execute() 
                .Should().Pass(); 
 
            string workingDirectory = Directory.GetParent(testProjectDirectory).FullName;

            new RunCommand() 
                .WithWorkingDirectory(workingDirectory) 
                .ExecuteWithCapturedOutput($"--no-build --project {Path.Combine(testProjectDirectory, testAppName)}.csproj") 
                .Should().Pass() 
                         .And.HaveStdOutContaining("Hello World!"); 
        } 
 
        [Fact] 
        public void It_runs_portable_apps_from_a_different_path_without_building() 
        { 
            var testAppName = "MSBuildTestApp"; 
            var testInstance = TestAssetsManager 
                .CreateTestInstance(testAppName); 
 
            var testProjectDirectory = testInstance.TestRoot; 
 
            new RestoreCommand() 
                .WithWorkingDirectory(testProjectDirectory) 
                .Execute() 
                .Should().Pass(); 
 
            string workingDirectory = Directory.GetParent(testProjectDirectory).FullName; 
            new RunCommand() 
                .WithWorkingDirectory(workingDirectory) 
                .ExecuteWithCapturedOutput($"--project {Path.Combine(testProjectDirectory, testAppName)}.csproj") 
                .Should().Pass() 
                         .And.HaveStdOutContaining("Hello World!"); 
        } 
    }
}