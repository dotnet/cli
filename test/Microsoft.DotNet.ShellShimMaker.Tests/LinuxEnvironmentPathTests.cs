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
    public class LinuxEnvironmentPathTests
    {
        [Fact]
        public void GivenenvironmentAndReporterItCanPrintOutInstructionToAddPath()
        {
            var fakeReporter = new FakeReporter();
            var linuxEnvironmentPath = new LinuxEnvironmentPath(
                @"executable\path",
                fakeReporter,
                new FakeEnvironmentProvider(
                    new Dictionary<string, string>
                    {
                        {"PATH", ""}
                    }),
                FakeFile.Empty);

            linuxEnvironmentPath.PrintAddPathInstructionIfPathDoesNotExist();

            // similar to https://code.visualstudio.com/docs/setup/mac
            fakeReporter.Message.Should().Be(
                $"Cannot find tools executable path in environment PATH. Please ensure executable\\path is added to your PATH.{Environment.NewLine}" +
                $"If you are using bash, you can add it by running following command:{Environment.NewLine}{Environment.NewLine}" +
                $"cat << EOF >> ~/.bash_profile{Environment.NewLine}" +
                $"# Add dotnet-sdk tools{Environment.NewLine}" +
                $"export PATH=\"$PATH:executable\\path\"{Environment.NewLine}" +
                $"EOF");
        }

        [Fact]
        public void GivenenvironmentAndReporterItPrintsNothingWhenenvironmentExists()
        {
            var fakeReporter = new FakeReporter();
            var linuxEnvironmentPath = new LinuxEnvironmentPath(
                @"executable\path",
                fakeReporter,
                new FakeEnvironmentProvider(
                    new Dictionary<string, string>
                    {
                        {"PATH", @"executable\path"}
                    }),
                FakeFile.Empty);

            linuxEnvironmentPath.PrintAddPathInstructionIfPathDoesNotExist();

            fakeReporter.Message.Should().BeEmpty();
        }

        [Fact]
        public void GivenAddPackageExecutablePathToUserPathJustRunItPrintsInstructionToLogout()
        {
            // arrange
            var fakeReporter = new FakeReporter();
            var linuxEnvironmentPath = new LinuxEnvironmentPath(
                @"executable\path",
                fakeReporter,
                new FakeEnvironmentProvider(
                    new Dictionary<string, string>
                    {
                        {"PATH", @""}
                    }),
                FakeFile.Empty);
            linuxEnvironmentPath.AddPackageExecutablePathToUserPath();

            // act
            linuxEnvironmentPath.PrintAddPathInstructionIfPathDoesNotExist();

            // asset
            fakeReporter.Message.Should().Be("You need logout to be able to run new installed command from shell");
        }
    }
}
