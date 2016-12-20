// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Tools.Test.Utilities;
using FluentAssertions;
using Microsoft.DotNet.TestFramework;
using Microsoft.DotNet.Cli.Utils;
using System.IO;
using System;

namespace Microsoft.DotNet.Cli.Test.Tests
{
    public class GivenDotnetTestBuildsAndRunsTestFromCsprojForMultipleTFM : TestBase
    {
        [WindowsOnlyFact(Skip="https://github.com/dotnet/cli/issues/4616")]
        public void MStestMultiTFM()
        {
            var testProjectDirectory = TestAssets.Get("VSTestDesktopAndNetCore")
                .CreateInstance()
                .WithSourceFiles()
                .WithNuGetConfig(new RepoDirectoriesProvider().TestPackages)
                .Root;
            
            var runtime = DotnetLegacyRuntimeIdentifiers.InferLegacyRestoreRuntimeIdentifier();

            new RestoreCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .WithRuntime(runtime)
                .Execute()
                .Should().Pass();

            var result = new DotnetTestCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .WithRuntime(runtime)
                .ExecuteWithCapturedOutput();

            result.StdOut
                .Should().Contain("Total tests: 3. Passed: 2. Failed: 1. Skipped: 0.", "because .NET 4.6 tests will pass")
                     .And.Contain("Passed   TestNamespace.VSTestTests.VSTestPassTestDesktop", "because .NET 4.6 tests will pass")
                     .And.Contain("Total tests: 3. Passed: 1. Failed: 2. Skipped: 0.", "because netcoreapp1.0 tests will fail")
                     .And.Contain("Failed   TestNamespace.VSTestTests.VSTestFailTestNetCoreApp", "because netcoreapp1.0 tests will fail");
            result.ExitCode.Should().Be(1);
        }

        [WindowsOnlyFact]
        public void XunitMultiTFM()
        {
            var testProjectDirectory = TestAssets
                .Get("VSTestXunitDesktopAndNetCore")
                .CreateInstance()
                .WithSourceFiles()
                .WithRestoreFiles()
                .Root;
                
            var result = new DotnetTestCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .ExecuteWithCapturedOutput();

            result.StdOut
                .Should().Contain("Total tests: 3. Passed: 2. Failed: 1. Skipped: 0.", "Because these are expected for Dekstop")
                     .And.Contain("Passed   TestNamespace.VSTestXunitTests.VSTestXunitPassTestDesktop", "Because these are expected for Dekstop")
                     .And.Contain("Total tests: 3. Passed: 1. Failed: 2. Skipped: 0.", "Because these are expected for Core")
                     .And.Contain("Failed   TestNamespace.VSTestXunitTests.VSTestXunitFailTestNetCoreApp", "Because these are expected for Core");

            result.ExitCode.Should().Be(1);
        }
    }
}
