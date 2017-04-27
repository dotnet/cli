// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;
using System;
using System.IO;
using FluentAssertions;
using Microsoft.DotNet.TestFramework;
using Microsoft.DotNet.Cli.Utils;

namespace Microsoft.DotNet.Cli.MSBuild.IntegrationTests
{
    public class GivenDotnetInvokesMSBuild : TestBase
    {
        [Theory]
        [InlineData("build")]
        [InlineData("clean")]
        [InlineData("msbuild")]
        [InlineData("pack")]
        [InlineData("publish")]
        [InlineData("test")]
        public void WhenDotnetCommandInvokesMsbuildThenEnvVarsAndMArePassed(string command)
        {
            var testInstance = TestAssets.Get("MSBuildIntegration")
                .CreateInstance(identifier: command)
                .WithSourceFiles();

            new DotnetCommand()
                .WithWorkingDirectory(testInstance.Root)
                .Execute(command)
                .Should().Pass();
        }

        [Theory]
        [InlineData("build")]
        [InlineData("clean")]
        [InlineData("msbuild")]
        [InlineData("pack")]
        [InlineData("publish")]
        public void WhenDotnetCommandInvokesMsbuildWithNoArgsVerbosityIsSetToNormal(string command)
        {
            var testInstance = TestAssets.Get("MSBuildIntegration")
                .CreateInstance(identifier: command)
                .WithSourceFiles();

            var cmd = new DotnetCommand()
                .WithWorkingDirectory(testInstance.Root)
                .ExecuteWithCapturedOutput(command);

            cmd.Should().Pass();

            cmd.StdOut
                .Should().Contain("Message with normal importance", "Because verbosity is set to normal")
                     .And.Contain("Message with high importance", "Because high importance messages are shown on normal verbosity")
                     .And.NotContain("Message with low importance", "Because low importance messages are not shown on normal verbosity");
        }

        [Theory]
        [InlineData("build")]
        [InlineData("clean")]
        [InlineData("pack")]
        [InlineData("publish")]
        public void WhenDotnetCommandInvokesMsbuildWithDiagVerbosityThenArgIsPassed(string command)
        {
            var testInstance = TestAssets.Get("MSBuildIntegration")
                .CreateInstance(identifier: command)
                .WithSourceFiles();

            var cmd = new DotnetCommand()
                .WithWorkingDirectory(testInstance.Root)
                .ExecuteWithCapturedOutput($"{command} -v diag");

            cmd.Should().Pass();

            cmd.StdOut.Should().Contain("Message with low importance");
        }

        [Fact]
        public void WhenDotnetTestInvokesMsbuildWithNoArgsVerbosityIsSetToQuiet()
        {
            var testInstance = TestAssets.Get("MSBuildIntegration")
                .CreateInstance()
                .WithSourceFiles();

            var cmd = new DotnetCommand()
                .WithWorkingDirectory(testInstance.Root)
                .ExecuteWithCapturedOutput("test");

            cmd.Should().Pass();
            cmd.StdOut.Should().NotContain("Message with high importance");
        }

        [Fact]
        public void WhenDotnetMsbuildCommandIsInvokedWithNonMsbuildSwitchThenItFails()
        {
            var testInstance = TestAssets.Get("MSBuildIntegration")
                .CreateInstance()
                .WithSourceFiles();

            var cmd = new DotnetCommand()
                .WithWorkingDirectory(testInstance.Root)
                .ExecuteWithCapturedOutput($"msbuild -v diag");

            cmd.ExitCode.Should().NotBe(0);
        }

        [Fact]
        public void WhenMSBuildSDKsPathIsSetByEnvVarThenItIsNotOverridden()
        {
            var testInstance = TestAssets.Get("MSBuildIntegration")
                .CreateInstance()
                .WithSourceFiles();

            var cmd = new DotnetCommand()
                .WithWorkingDirectory(testInstance.Root)
                .WithEnvironmentVariable("MSBuildSDKsPath", "AnyString")
                .ExecuteWithCapturedOutput($"msbuild");

            cmd.ExitCode.Should().NotBe(0);

            cmd.StdOut.Should().Contain("Expected 'AnyString")
                           .And.Contain("to exist, but it does not.");
        }
    }
}
