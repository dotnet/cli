// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Construction;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;
using FluentAssertions;

namespace Microsoft.DotNet.New.Tests
{
    public class GivenThatIWantANewApp : TestBase
    {
        [Fact]
        public void WhenDotnetNewIsInvokedMultipleTimesItShouldFail()
        {
            var testDirectory = TestAssets.CreateTestDirectory();

            new TestCommand("dotnet")
                .WithWorkingDirectory(testDirectory)
                .Execute("new");

            var expectedState = testDirectory.LastWriteTime;

            var result = new TestCommand("dotnet")
                .WithWorkingDirectory(testDirectory)
                .ExecuteWithCapturedOutput("new");

            var actualState = testDirectory.LastWriteTime;

            Assert.Equal(expectedState, actualState);

            result
                .Should().Fail()
                     .And.HaveStdErr();
        }
 
        [Fact] 
        public void RestoreDoesNotUseAnyCliProducedPackagesOnItsTemplates() 
        { 
            var cSharpTemplates = new [] { "Console", "Lib", "Web", "Mstest", "XUnittest" }; 
 
            var testDirectory = TestAssets.CreateTestDirectory();

            var packagesDirectory = testDirectory.GetDirectory("packages");
 
            foreach (var cSharpTemplate in cSharpTemplates)
            {
                var projectDirectory = testDirectory.GetDirectory(cSharpTemplate);

                projectDirectory.Create();

                CreateAndRestoreNewProject(cSharpTemplate, projectDirectory, packagesDirectory); 
            } 
 
            packagesDirectory
                .Should().NotHaveFilesMatching($"Microsoft.DotNet.Cli.Utils.*.nupkg", SearchOption.AllDirectories); 
        } 
 
        private void CreateAndRestoreNewProject( 
            string projectType, 
            DirectoryInfo projectDirectory, 
            DirectoryInfo packagesDirectory) 
        { 
            new TestCommand("dotnet")
                .WithWorkingDirectory(projectDirectory)
                .Execute($"new --type {projectType}")
                .Should().Pass();
 
            new RestoreCommand() 
                .WithWorkingDirectory(projectDirectory)
                .Execute($"--packages {packagesDirectory.FullName} /p:SkipInvalidConfigurations=true")
                .Should().Pass();
        } 
    }
}
