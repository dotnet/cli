// Copyright (c) .NET Foundation and contributors. All rights reserved.
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
                                   configuration, "netcoreapp2.1", "VSTestCore.dll");
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
        public void ItCreatesCoverageFileWhenCodeCoverageEnabledByRunsettings()
        {
            var testProjectDirectory = this.CopyAndRestoreVSTestDotNetCoreTestApp("12");

            string trxLoggerDirectory = Path.Combine(testProjectDirectory, "RD");

            // Delete trxLoggerDirectory if it exist
            if (Directory.Exists(trxLoggerDirectory))
            {
                Directory.Delete(trxLoggerDirectory, true);
            }

            var settingsPath =Path.Combine(AppContext.BaseDirectory, "CollectCodeCoverage.runsettings");

            // Call test
            CommandResult result = new DotnetTestCommand()
                                        .WithWorkingDirectory(testProjectDirectory)
                                        .ExecuteWithCapturedOutput(
                                            "--settings " + settingsPath
                                            + " --logger \"trx;logfilename=custom.trx\" "
                                            + "--results-directory " + trxLoggerDirectory);

            // Verify test results
            if (!DotnetUnderTest.IsLocalized())
            {
                result.StdOut.Should().Contain("Total tests: 2. Passed: 1. Failed: 1. Skipped: 0.");
            }

            // Verify trx file.
            var trxFilePath = Path.Combine(trxLoggerDirectory, "custom.trx");
            Assert.True(File.Exists(trxFilePath));
            result.StdOut.Should().Contain(trxFilePath);

            // Verify coverage file.
            var coverageFilePath = GivenDotnettestBuildsAndRunsTestfromCsproj.GetCoverageFileNameFromTrx(trxFilePath);
            result.StdOut.Should().Contain(Path.GetFileName(coverageFilePath));
            Assert.True(File.Exists(coverageFilePath), $"Coverage file: {coverageFilePath} not found.");

            result.ExitCode.Should().Be(1);
        }

        [Fact(Skip = "Code coverage with default runsettings failing on jenkins CI, fix tracking https://github.com/Microsoft/vstest/pull/1619")]
        public void ItCreatesCoverageFileInResultsDirectory()
        {
            var testProjectDirectory = this.CopyAndRestoreVSTestDotNetCoreTestApp("11");

            string trxLoggerDirectory = Path.Combine(testProjectDirectory, "RD");

            // Delete trxLoggerDirectory if it exist
            if (Directory.Exists(trxLoggerDirectory))
            {
                Directory.Delete(trxLoggerDirectory, true);
            }

            // Call test
            CommandResult result = new DotnetTestCommand()
                                        .WithWorkingDirectory(testProjectDirectory)
                                        .ExecuteWithCapturedOutput(
                                            "--collect \"Code Coverage\" "
                                            + "--logger \"trx;logfilename=custom.trx\" "
                                            + "--results-directory " + trxLoggerDirectory);

            // Verify test results
            if (!DotnetUnderTest.IsLocalized())
            {
                result.StdOut.Should().Contain("Total tests: 2. Passed: 1. Failed: 1. Skipped: 0.");
            }

            // Verify trx file.
            var trxFilePath = Path.Combine(trxLoggerDirectory, "custom.trx");
            Assert.True(File.Exists(trxFilePath));
            result.StdOut.Should().Contain(trxFilePath);

            // Verify coverage file.
            var coverageFilePath = GivenDotnettestBuildsAndRunsTestfromCsproj.GetCoverageFileNameFromTrx(trxFilePath);
            result.StdOut.Should().Contain(Path.GetFileName(coverageFilePath));
            Assert.True(File.Exists(coverageFilePath), $"Coverage file: {coverageFilePath} not found.");

            result.ExitCode.Should().Be(1);
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

        private static string GetCoverageFileNameFromTrx(string trxFilePath)
        {
            Assert.True(File.Exists(trxFilePath), $"Trx file not found: {trxFilePath}");

            XmlDocument doc = new XmlDocument();
            using (var trxStream = new FileStream(trxFilePath, FileMode.Open, FileAccess.Read))
            {
                doc.Load(trxStream);
                var deploymentElements = doc.GetElementsByTagName("Deployment");
                Assert.True(deploymentElements.Count == 1,
                    $"None or more than one Deployment tags found in trx file:{trxFilePath}");

                var deploymentDir = deploymentElements[0].Attributes.GetNamedItem("runDeploymentRoot")?.Value;
                Assert.True(string.IsNullOrEmpty(deploymentDir) == false,
                    $"runDeploymentRoot attatribute not found in trx file:{trxFilePath}");
                var collectors = doc.GetElementsByTagName("Collector");

                string fileName = string.Empty;
                for (int i = 0; i < collectors.Count; i++)
                {
                    if (string.Equals(collectors[i].Attributes.GetNamedItem("collectorDisplayName").Value,
                        "Code Coverage", StringComparison.OrdinalIgnoreCase))
                    {
                        fileName = collectors[i].FirstChild?.FirstChild?.FirstChild?.Attributes.GetNamedItem("href")
                            ?.Value;
                    }
                }

                Assert.True(string.IsNullOrEmpty(fileName) == false, $"Coverage file name not found in trx file: {trxFilePath}");
                var resultsDirectory = Path.GetDirectoryName(trxFilePath);
                return Path.Combine(resultsDirectory, deploymentDir, "In", fileName);
            }
        }
    }
}
