// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using FluentAssertions;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.TestFramework;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;

namespace Microsoft.DotNet.Cli.Test.Tests
{
    public class GivenDotnettestBuildsAndRunsTestfromCsproj : TestBase
    {
        [Fact]
        public void WhenTestingMSTestProjectWithSingleTFMThenResultsAreProduced()
        {
            var testProjectDirectory = TestAssets.Get("VSTestDotNetCore")
                .CreateInstance()
                .WithSourceFiles()
                .Root;
                
            new RestoreCommand()
                .WithWorkingDirectory(testProjectDirectory)
				.Execute()
				.Should().Pass();

            var result = new DotnetTestCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .ExecuteWithCapturedOutput();

            result.StdOut
                .Should().Contain("Total tests: 2. Passed: 1. Failed: 1. Skipped: 0.")
                     .And.Contain("Passed   TestNamespace.VSTestTests.VSTestPassTest")
                     .And.Contain("Failed   TestNamespace.VSTestTests.VSTestFailTest");

            result.ExitCode.Should().Be(1);
        }

        [Fact]
        public void WhenTestingXunitProjectWithSingleTFMThenResultsAreProduced()
        {
            var testProjectDirectory = TestAssets.Get("VSTestXunitDotNetCore")
                .CreateInstance()
                .WithSourceFiles()
                .WithRestoreFiles()
                .Root;

            var result = new DotnetTestCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .ExecuteWithCapturedOutput();

            result.StdOut.Should().Contain("Total tests: 2. Passed: 1. Failed: 1. Skipped: 0.")
                              .And.Contain("Passed   TestNamespace.VSTestXunitTests.VSTestXunitPassTest")
                              .And.Contain("Failed   TestNamespace.VSTestXunitTests.VSTestXunitFailTest");

            result.ExitCode.Should().Be(1);
        }

        [Fact]
        public void WhenNoBuildOptionIsGivenThenTestWillNotBuild()
        {
            var testProjectDirectory = TestAssets.Get("VSTestDotNetCore")
                .CreateInstance()
                .WithSourceFiles()
                .WithRestoreFiles()
                .Root;

            var configuration = Environment.GetEnvironmentVariable("CONFIGURATION") ?? "Debug";

            var buildArtifact = Path.Combine(testProjectDirectory, "bin",
                                   configuration, "netcoreapp1.0", "VSTestDotNetCore.dll");

            var result = new DotnetTestCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .ExecuteWithCapturedOutput("--no-build");

            result.StdOut
                .Should().Pass()
                     .And.Contain(expectedError); 
        }

        [Fact]
        public void WhenLoggerOptionGivenThenTestUsesThatLogger()
        {
            var testProjectDirectory = TestAssets.Get("VSTestDotNetCore")
                .CreateInstance()
                .WithSourceFiles()
                .WithRestoreFiles()
                .Root;

            var trxLoggerDirectory = testProjectDirectory.GetDirectory("TestResults");

            if(trxLoggerDirectory.Exists)
            {
                trxLoggerDirectory.Delete(true);
            }

            var result = new DotnetTestCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .ExecuteWithCapturedOutput("--logger:trx");

            
            var trxFiles = trxLoggerDirectory.GetFiles("*.trx");

            trxFiles.Length
                .Should().Be(1, "Because a single trx file should have been produced");

            result.StdOut.Should().Contain(trxFiles[0]);
        }

        [Fact(Skip = "https://github.com/dotnet/cli/issues/5035")]
        public void ItBuildsAndTestsAppWhenRestoringToSpecificDirectory()
        {
            var rootPath = TestAssets.Get("VSTestDotNetCore").CreateInstance().WithSourceFiles().Root.FullName;

            string dir = "pkgs";
            string fullPath = Path.GetFullPath(Path.Combine(rootPath, dir));

            string args = $"--packages \"{dir}\"";
            new RestoreCommand()
                .WithWorkingDirectory(rootPath)
                .Execute(args)
                .Should()
                .Pass();

            new BuildCommand()
                .WithWorkingDirectory(rootPath)
                .ExecuteWithCapturedOutput()
                .Should()
                .Pass()
                .And.NotHaveStdErr();

            CommandResult result = new DotnetTestCommand()
                                        .WithWorkingDirectory(rootPath)
                                        .ExecuteWithCapturedOutput();

            result.StdOut.Should().Contain("Total tests: 2. Passed: 1. Failed: 1. Skipped: 0.");
            result.StdOut.Should().Contain("Passed   TestNamespace.VSTestTests.VSTestPassTest");
            result.StdOut.Should().Contain("Failed   TestNamespace.VSTestTests.VSTestFailTest");
        }
    }
}
