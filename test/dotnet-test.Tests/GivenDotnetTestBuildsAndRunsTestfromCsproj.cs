﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;
using FluentAssertions;
using Microsoft.DotNet.TestFramework;
using Microsoft.DotNet.Cli.Utils;
using System.IO;
using System;
using System.Runtime.CompilerServices;
using System.Xml;

namespace Microsoft.DotNet.Cli.Test.Tests
{
    public class GivenDotnettestBuildsAndRunsTestfromCsproj : TestBase
    {
        [Fact]
        public void MSTestSingleTFM()
        {
            var testProjectDirectory = this.CopyAndRestoreVSTestDotNetCoreTestApp("3");

            // Call test
            CommandResult result = new DotnetTestCommand()
                                        .WithWorkingDirectory(testProjectDirectory)
                                        .ExecuteWithCapturedOutput(TestBase.ConsoleLoggerOutputNormal);

            // Verify
            if (!DotnetUnderTest.IsLocalized())
            {
                result.StdOut.Should().Contain("Total tests: 2. Passed: 1. Failed: 1. Skipped: 0.");
                result.StdOut.Should().Contain("Passed   VSTestPassTest");
                result.StdOut.Should().Contain("Failed   VSTestFailTest");
            }

            result.ExitCode.Should().Be(1);
        }

        [Fact]
        public void ItImplicitlyRestoresAProjectWhenTesting()
        {
            string testAppName = "VSTestCore";
            var testInstance = TestAssets.Get(testAppName)
                            .CreateInstance()
                            .WithSourceFiles();

            var testProjectDirectory = testInstance.Root.FullName;

            CommandResult result = new DotnetTestCommand()
                                        .WithWorkingDirectory(testProjectDirectory)
                                        .ExecuteWithCapturedOutput(TestBase.ConsoleLoggerOutputNormal);

            if (!DotnetUnderTest.IsLocalized())
            {
                result.StdOut.Should().Contain("Total tests: 2. Passed: 1. Failed: 1. Skipped: 0.");
                result.StdOut.Should().Contain("Passed   VSTestPassTest");
                result.StdOut.Should().Contain("Failed   VSTestFailTest");
            }

            result.ExitCode.Should().Be(1);
        }

        [Fact]
        public void ItDoesNotImplicitlyRestoreAProjectWhenTestingWithTheNoRestoreOption()
        {
            string testAppName = "VSTestCore";
            var testInstance = TestAssets.Get(testAppName)
                            .CreateInstance()
                            .WithSourceFiles();

            var testProjectDirectory = testInstance.Root.FullName;

            new DotnetTestCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .ExecuteWithCapturedOutput($"{TestBase.ConsoleLoggerOutputNormal} --no-restore")
                .Should().Fail()
                .And.HaveStdOutContaining("project.assets.json");
        }

        [Fact]
        public void XunitSingleTFM()
        {
            // Copy XunitCore project in output directory of project dotnet-vstest.Tests
            string testAppName = "XunitCore";
            var testInstance = TestAssets.Get(testAppName)
                            .CreateInstance("4")
                            .WithSourceFiles();

            var testProjectDirectory = testInstance.Root.FullName;

            // Restore project XunitCore
            new RestoreCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .Execute()
                .Should()
                .Pass();

            // Call test
            CommandResult result = new DotnetTestCommand()
                                        .WithWorkingDirectory(testProjectDirectory)
                                        .ExecuteWithCapturedOutput(TestBase.ConsoleLoggerOutputNormal);

            // Verify
            if (!DotnetUnderTest.IsLocalized())
            {
                result.StdOut.Should().Contain("Total tests: 2. Passed: 1. Failed: 1. Skipped: 0.");
                result.StdOut.Should().Contain("Passed   TestNamespace.VSTestXunitTests.VSTestXunitPassTest");
                result.StdOut.Should().Contain("Failed   TestNamespace.VSTestXunitTests.VSTestXunitFailTest");
            }

            result.ExitCode.Should().Be(1);
        }

        [Fact]
        public void GivenAFailingTestItDisplaysFailureDetails()
        {
            var testInstance = TestAssets.Get("XunitCore")
                .CreateInstance()
                .WithSourceFiles();

            var result = new DotnetTestCommand()
                .WithWorkingDirectory(testInstance.Root.FullName)
                .ExecuteWithCapturedOutput();

            result.ExitCode.Should().Be(1);

            if (!DotnetUnderTest.IsLocalized())
            {
                result.StdOut.Should().Contain("Failed   TestNamespace.VSTestXunitTests.VSTestXunitFailTest");
                result.StdOut.Should().Contain("Assert.Equal() Failure");
                result.StdOut.Should().Contain("Total tests: 2. Passed: 1. Failed: 1. Skipped: 0.");
            }
        }

        [Fact]
        public void ItAcceptsMultipleLoggersAsCliArguments()
        {
            // Copy and restore VSTestCore project in output directory of project dotnet-vstest.Tests
            var testProjectDirectory = this.CopyAndRestoreVSTestDotNetCoreTestApp("10");

            string trxLoggerDirectory = Path.Combine(testProjectDirectory, "RD");

            // Delete trxLoggerDirectory if it exist
            if (Directory.Exists(trxLoggerDirectory))
            {
                Directory.Delete(trxLoggerDirectory, true);
            }

            // Call test with logger enable
            CommandResult result = new DotnetTestCommand()
                                       .WithWorkingDirectory(testProjectDirectory)
                                       .ExecuteWithCapturedOutput("--logger \"trx;logfilename=custom.trx\" --logger console;verbosity=normal -- RunConfiguration.ResultsDirectory=" + trxLoggerDirectory);

            // Verify
            var trxFilePath = Path.Combine(trxLoggerDirectory, "custom.trx");
            Assert.True(File.Exists(trxFilePath));
            result.StdOut.Should().Contain(trxFilePath);
            result.StdOut.Should().Contain("Passed   VSTestPassTest");
            result.StdOut.Should().Contain("Failed   VSTestFailTest");

            // Cleanup trxLoggerDirectory if it exist
            if (Directory.Exists(trxLoggerDirectory))
            {
                Directory.Delete(trxLoggerDirectory, true);
            }
        }

        [Fact]
        public void TestWillNotBuildTheProjectIfNoBuildArgsIsGiven()
        {
            // Copy and restore VSTestCore project in output directory of project dotnet-vstest.Tests
            var testProjectDirectory = this.CopyAndRestoreVSTestDotNetCoreTestApp("5");
            string configuration = Environment.GetEnvironmentVariable("CONFIGURATION") ?? "Debug";
            string expectedError = Path.Combine(testProjectDirectory, "bin",
                                   configuration, "netcoreapp3.0", "VSTestCore.dll");
            expectedError = "The test source file " + "\"" + expectedError + "\"" + " provided was not found.";

            // Call test
            CommandResult result = new DotnetTestCommand()
                                       .WithWorkingDirectory(testProjectDirectory)
                                       .ExecuteWithCapturedOutput("--no-build -v:m");

            // Verify
            if (!DotnetUnderTest.IsLocalized())
            {
                result.StdOut.Should().NotContain("Restore");
                result.StdErr.Should().Contain(expectedError);
            }

            result.ExitCode.Should().Be(1);
        }

        [Fact]
        public void TestWillCreateTrxLoggerInTheSpecifiedResultsDirectoryBySwitch()
        {
            // Copy and restore VSTestCore project in output directory of project dotnet-vstest.Tests
            var testProjectDirectory = this.CopyAndRestoreVSTestDotNetCoreTestApp("6");

            string trxLoggerDirectory = Path.Combine(testProjectDirectory, "TR", "x.y");

            // Delete trxLoggerDirectory if it exist
            if (Directory.Exists(trxLoggerDirectory))
            {
                Directory.Delete(trxLoggerDirectory, true);
            }

            // Call test with trx logger enabled and results directory explicitly specified.
            CommandResult result = new DotnetTestCommand()
                                       .WithWorkingDirectory(testProjectDirectory)
                                       .ExecuteWithCapturedOutput("--logger trx -r \"" + trxLoggerDirectory + "\"");

            // Verify
            String[] trxFiles = Directory.GetFiles(trxLoggerDirectory, "*.trx");
            Assert.Equal(1, trxFiles.Length);
            result.StdOut.Should().Contain(trxFiles[0]);

            // Cleanup trxLoggerDirectory if it exist
            if(Directory.Exists(trxLoggerDirectory))
            {
                Directory.Delete(trxLoggerDirectory, true);
            }
        }

        [Fact]
        public void ItCreatesTrxReportInTheSpecifiedResultsDirectoryByArgs()
        {
            // Copy and restore VSTestCore project in output directory of project dotnet-vstest.Tests
            var testProjectDirectory = this.CopyAndRestoreVSTestDotNetCoreTestApp("7");

            string trxLoggerDirectory = Path.Combine(testProjectDirectory, "RD");

            // Delete trxLoggerDirectory if it exist
            if (Directory.Exists(trxLoggerDirectory))
            {
                Directory.Delete(trxLoggerDirectory, true);
            }

            // Call test with logger enable
            CommandResult result = new DotnetTestCommand()
                                       .WithWorkingDirectory(testProjectDirectory)
                                       .ExecuteWithCapturedOutput("--logger \"trx;logfilename=custom.trx\" -- RunConfiguration.ResultsDirectory=" + trxLoggerDirectory);

            // Verify
            var trxFilePath = Path.Combine(trxLoggerDirectory, "custom.trx");
            Assert.True(File.Exists(trxFilePath));
            result.StdOut.Should().Contain(trxFilePath);

            // Cleanup trxLoggerDirectory if it exist
            if (Directory.Exists(trxLoggerDirectory))
            {
                Directory.Delete(trxLoggerDirectory, true);
            }
        }

        [Fact]
        public void ItBuildsAndTestsAppWhenRestoringToSpecificDirectory()
        {
            // Creating folder with name short name "RestoreTest" to avoid PathTooLongException
            var rootPath = TestAssets.Get("VSTestCore").CreateInstance("8").WithSourceFiles().Root.FullName;

            // Moving pkgs folder on top to avoid PathTooLongException
            string dir = @"..\..\..\..\pkgs";
            string fullPath = Path.GetFullPath(Path.Combine(rootPath, dir));

            string args = $"--packages \"{dir}\"";
            new RestoreCommand()
                .WithWorkingDirectory(rootPath)
                .Execute(args)
                .Should()
                .Pass();

            new BuildCommand()
                .WithWorkingDirectory(rootPath)
                .ExecuteWithCapturedOutput("--no-restore")
                .Should()
                .Pass()
                .And.NotHaveStdErr();

            CommandResult result = new DotnetTestCommand()
                                        .WithWorkingDirectory(rootPath)
                                        .ExecuteWithCapturedOutput($"{TestBase.ConsoleLoggerOutputNormal} --no-restore");

            if (!DotnetUnderTest.IsLocalized())
            {
                result.StdOut.Should().Contain("Total tests: 2. Passed: 1. Failed: 1. Skipped: 0.");
                result.StdOut.Should().Contain("Passed   VSTestPassTest");
                result.StdOut.Should().Contain("Failed   VSTestFailTest");
            }

            result.ExitCode.Should().Be(1);
        }

        [Fact]
        public void ItUsesVerbosityPassedToDefineVerbosityOfConsoleLoggerOfTheTests()
        {
            // Copy and restore VSTestCore project in output directory of project dotnet-vstest.Tests
            var testProjectDirectory = this.CopyAndRestoreVSTestDotNetCoreTestApp("9");

            // Call test
            CommandResult result = new DotnetTestCommand()
                                        .WithWorkingDirectory(testProjectDirectory)
                                        .ExecuteWithCapturedOutput("-v q");

            // Verify
            if (!DotnetUnderTest.IsLocalized())
            {
                result.StdOut.Should().Contain("Total tests: 2. Passed: 1. Failed: 1. Skipped: 0.");
                result.StdOut.Should().NotContain("Passed   TestNamespace.VSTestTests.VSTestPassTest");
                result.StdOut.Should().NotContain("Failed   TestNamespace.VSTestTests.VSTestFailTest");
            }

            result.ExitCode.Should().Be(1);
        }

        [Fact]
        public void ItTestsWithTheSpecifiedRuntimeOption()
        {
            var testInstance = TestAssets.Get("XunitCore")
                            .CreateInstance()
                            .WithSourceFiles();

            var rootPath = testInstance.Root.FullName;
            var rid = DotnetLegacyRuntimeIdentifiers.InferLegacyRestoreRuntimeIdentifier();

            new BuildCommand()
                .WithWorkingDirectory(rootPath)
                .ExecuteWithCapturedOutput($"--runtime {rid}")
                .Should()
                .Pass()
                .And.NotHaveStdErr();

            var result = new DotnetTestCommand()
                .WithWorkingDirectory(rootPath)
                .ExecuteWithCapturedOutput($"{TestBase.ConsoleLoggerOutputNormal} --no-build --runtime {rid}");

            result
                .Should()
                .NotHaveStdErrContaining("MSB1001")
                .And
                .HaveStdOutContaining(rid);

            if (!DotnetUnderTest.IsLocalized())
            {
                result
                    .Should()
                    .HaveStdOutContaining("Total tests: 2. Passed: 1. Failed: 1. Skipped: 0.")
                    .And
                    .HaveStdOutContaining("Passed   TestNamespace.VSTestXunitTests.VSTestXunitPassTest")
                    .And
                    .HaveStdOutContaining("Failed   TestNamespace.VSTestXunitTests.VSTestXunitFailTest");
            }

            result.ExitCode.Should().Be(1);
        }

        [WindowsOnlyFact]
        public void ItCreatesCoverageFileWhenCodeCoverageEnabledByRunsettings()
        {
            var testProjectDirectory = this.CopyAndRestoreVSTestDotNetCoreTestApp("11");

            string resultsDirectory = Path.Combine(testProjectDirectory, "RD");

            // Delete resultsDirectory if it exist
            if (Directory.Exists(resultsDirectory))
            {
                Directory.Delete(resultsDirectory, true);
            }

            var settingsPath =Path.Combine(AppContext.BaseDirectory, "CollectCodeCoverage.runsettings");

            // Call test
            CommandResult result = new DotnetTestCommand()
                                        .WithWorkingDirectory(testProjectDirectory)
                                        .ExecuteWithCapturedOutput(
                                            "--settings " + settingsPath
                                            + " --results-directory " + resultsDirectory);

            // Verify test results
            if (!DotnetUnderTest.IsLocalized())
            {
                result.StdOut.Should().Contain("Total tests: 2. Passed: 1. Failed: 1. Skipped: 0.");
            }

            // Verify coverage file.
            DirectoryInfo d = new DirectoryInfo(resultsDirectory);
            FileInfo[] coverageFileInfos = d.GetFiles("*.coverage", SearchOption.AllDirectories);
            Assert.Equal(1, coverageFileInfos.Length);

            result.ExitCode.Should().Be(1);
        }

        [WindowsOnlyFact]
        public void ItCreatesCoverageFileInResultsDirectory()
        {
            var testProjectDirectory = this.CopyAndRestoreVSTestDotNetCoreTestApp("12");

            string resultsDirectory = Path.Combine(testProjectDirectory, "RD");

            // Delete resultsDirectory if it exist
            if (Directory.Exists(resultsDirectory))
            {
                Directory.Delete(resultsDirectory, true);
            }

            // Call test
            CommandResult result = new DotnetTestCommand()
                                        .WithWorkingDirectory(testProjectDirectory)
                                        .ExecuteWithCapturedOutput(
                                            "--collect \"Code Coverage\" "
                                            + "--results-directory " + resultsDirectory);

            // Verify test results
            if (!DotnetUnderTest.IsLocalized())
            {
                result.StdOut.Should().Contain("Total tests: 2. Passed: 1. Failed: 1. Skipped: 0.");
            }

            // Verify coverage file.
            DirectoryInfo d = new DirectoryInfo(resultsDirectory);
            FileInfo[] coverageFileInfos = d.GetFiles("*.coverage", SearchOption.AllDirectories);
            Assert.Equal(1, coverageFileInfos.Length);

            result.ExitCode.Should().Be(1);
        }

        [UnixOnlyFact]
        public void ItShouldShowWarningMessageOnCollectCodeCoverage()
        {
            var testProjectDirectory = this.CopyAndRestoreVSTestDotNetCoreTestApp("13");

            // Call test
            CommandResult result = new DotnetTestCommand()
                                        .WithWorkingDirectory(testProjectDirectory)
                                        .ExecuteWithCapturedOutput(
                                            "--collect \"Code Coverage\" "
                                            + "--filter VSTestPassTest");

            // Verify test results
            if (!DotnetUnderTest.IsLocalized())
            {
                result.StdOut.Should().Contain("No code coverage data available. Code coverage is currently supported only on Windows.");
                result.StdOut.Should().Contain("Total tests: 1. Passed: 1. Failed: 0. Skipped: 0.");
                result.StdOut.Should().Contain("Test Run Successful.");
            }

            result.ExitCode.Should().Be(0);
        }

        private string CopyAndRestoreVSTestDotNetCoreTestApp([CallerMemberName] string callingMethod = "")
        {
            // Copy VSTestCore project in output directory of project dotnet-vstest.Tests
            string testAppName = "VSTestCore";
            var testInstance = TestAssets.Get(testAppName)
                            .CreateInstance(callingMethod)
                            .WithSourceFiles();

            var testProjectDirectory = testInstance.Root.FullName;

            // Restore project VSTestCore
            new RestoreCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .Execute()
                .Should()
                .Pass();

            return testProjectDirectory;
        }
    }
}
