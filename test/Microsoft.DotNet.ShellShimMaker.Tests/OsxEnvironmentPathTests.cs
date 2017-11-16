// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using FluentAssertions;
using Microsoft.DotNet.Tools.Test.Utilities;
using Microsoft.Extensions.DependencyModel.Tests;
using Xunit;

namespace Microsoft.DotNet.ShellShimMaker.Tests
{
    public class OsxEnvironmentPathTests
    {
        [Fact]
        public void GivenEnvironementAndReporterItCanPrintOutInstructionToAddPath()
        {
            var fakeReporter = new FakeReporter();
            var osxEnvironmentPath = new OsxEnvironmentPath(
                @"~/executable/path",
                @"/Users/name/executable/path",
                fakeReporter,
                new FakeEnvironmentProvider(
                    new Dictionary<string, string>
                    {
                        {"PATH", ""}
                    }),
                FakeFile.Empty);

            osxEnvironmentPath.PrintAddPathInstructionIfPathDoesNotExist();

            // similar to https://code.visualstudio.com/docs/setup/mac
            fakeReporter.Message.Should().Be(
                $"Cannot find tools executable path in environement PATH. Please ensure /Users/name/executable/path is added to your PATH.{Environment.NewLine}" +
                $"If you are using bash, you can add it by running following command:{Environment.NewLine}{Environment.NewLine}" +
                $"cat << EOF >> ~/.bash_profile{Environment.NewLine}" +
                $"# Add dotnet-sdk tools{Environment.NewLine}" +
                $"export PATH=\"$PATH:/Users/name/executable/path\"{Environment.NewLine}" +
                $"EOF");
        }

        [Theory]
        [InlineData("/Users/name/executable/path")]
        [InlineData("~/executable/path")]
        public void GivenEnvironementAndReporterItPrintsNothingWhenEnvironementExists(string existingPath)
        {
            var fakeReporter = new FakeReporter();
            var osxEnvironmentPath = new OsxEnvironmentPath(
                @"~/executable/path",
                @"/Users/name/executable/path",
                fakeReporter,
                new FakeEnvironmentProvider(
                    new Dictionary<string, string>
                    {
                        {"PATH", existingPath}
                    }),
                FakeFile.Empty);

            osxEnvironmentPath.PrintAddPathInstructionIfPathDoesNotExist();

            fakeReporter.Message.Should().BeEmpty();
        }

        [Fact]
        public void GivenAddPackageExecutablePathToUserPathJustRunItPrintsInstructionToLogout()
        {
            // arrange
            var fakeReporter = new FakeReporter();
            var osxEnvironmentPath = new OsxEnvironmentPath(
                @"~/executable/path",
                @"/Users/name/executable/path",
                fakeReporter,
                new FakeEnvironmentProvider(
                    new Dictionary<string, string>
                    {
                        {"PATH", @""}
                    }),
                FakeFile.Empty);
            osxEnvironmentPath.AddPackageExecutablePathToUserPath();

            // act
            osxEnvironmentPath.PrintAddPathInstructionIfPathDoesNotExist();

            // asset
            fakeReporter.Message.Should().Be(
                $"You need reopen to be able to run new installed command from shell{Environment.NewLine}" +
                $"If you are using different a shell that is not sh or bash, you need to ensure /Users/name/executable/path is in your path");
        }
    }
}
