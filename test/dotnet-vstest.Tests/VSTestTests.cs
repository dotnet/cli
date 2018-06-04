﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;
using System;
using System.IO;
using FluentAssertions;
using Microsoft.DotNet.TestFramework;
using Microsoft.DotNet.Cli.Utils;

namespace Microsoft.DotNet.Cli.VSTest.Tests
{
    public class VSTestTests : TestBase
    {
        [Fact]
        public void TestsFromAGivenContainerShouldRunWithExpectedOutput()
        {
            var testAppName = "VSTestCore";
            var testRoot = TestAssets.Get(testAppName)
                .CreateInstance()
                .WithSourceFiles()
                .WithRestoreFiles()
                .Root;

            var configuration = Environment.GetEnvironmentVariable("CONFIGURATION") ?? "Debug";

            new BuildCommand()
                .WithWorkingDirectory(testRoot)
                .Execute()
                .Should().Pass();

            var outputDll = testRoot
                .GetDirectory("bin", configuration, "netcoreapp2.2")
                .GetFile($"{testAppName}.dll");

            var argsForVstest = $"\"{outputDll.FullName}\" --logger:console;verbosity=normal";

            // Call vstest
            var result = new VSTestCommand().ExecuteWithCapturedOutput(argsForVstest);
            if (!DotnetUnderTest.IsLocalized())
            {
                result.StdOut
                    .Should().Contain("Total tests: 2. Passed: 1. Failed: 1. Skipped: 0.")
                    .And.Contain("Passed   VSTestPassTest")
                    .And.Contain("Failed   VSTestFailTest");
            }

            result.ExitCode.Should().Be(1);
        }
    }
}
