// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;

namespace Microsoft.DotNet.Tests.Commands
{
    public class CompleteCommandTests : TestBase
    {
        [Fact]
        public void GivenOnlyDotnetItSuggestsTopLevelCommandsAndOptions()
        {
            var expected = new string[] {
                "--diagnostics",
                "--help",
                "--info",
                "--list-runtimes",
                "--list-sdks",
                "--version",
                "-d",
                "-h",
                "add",
                "build",
                "build-server",
                "clean",
                "help",
                "list",
                "migrate",
                "msbuild",
                "new",
                "nuget",
                "pack",
                "publish",
                "remove",
                "restore",
                "run",
                "sln",
                "store",
                "test",
                "tool",
                "vstest"
            };

            var reporter = new BufferedReporter();
            CompleteCommand.RunWithReporter(new[] { "dotnet " }, reporter).Should().Be(0);
            reporter.Lines.Should().Equal(expected.OrderBy(c => c));
        }

        [Fact]
        public void GivenASlashItSuggestsTopLevelOptions()
        {
            var expected = new string[] {
                "--diagnostics",
                "--help",
                "--info",
                "--list-runtimes",
                "--list-sdks",
                "--version",
                "-d",
                "-h",
                "build-server" // This should be removed when completion is based on "starts with" rather than "contains".
                               // See https://github.com/dotnet/cli/issues/8958.
            };

            var reporter = new BufferedReporter();
            CompleteCommand.RunWithReporter(new[] { "dotnet -" }, reporter).Should().Be(0);
            reporter.Lines.Should().Equal(expected.OrderBy(c => c));
        }

        [Fact(Skip = "https://github.com/dotnet/cli/issues/9737")]
        public void GivenNewCommandItDisplaysCompletions()
        {
            var expected = new string[] {
                "--dry-run",
                "--force",
                "--help",
                "--install",
                "--language",
                "--list",
                "--name",
                "--nuget-source",
                "--output",
                "--type",
                "--uninstall",
                "-h",
                "-i",
                "-l",
                "-lang",
                "-n",
                "-o",
                "-u"
            };

            var reporter = new BufferedReporter();
            CompleteCommand.RunWithReporter(new[] { "dotnet new " }, reporter).Should().Be(0);
            reporter.Lines.Should().Equal(expected.OrderBy(c => c));
        }

        [Fact]
        public void GivenNuGetCommandItDisplaysCompletions()
        {
            var expected = new string[] {
                "--help",
                "--verbosity",
                "--version",
                "-h",
                "-v",
                "delete",
                "locals",
                "push",
            };

            var reporter = new BufferedReporter();
            CompleteCommand.RunWithReporter(new[] { "dotnet nuget " }, reporter).Should().Be(0);
            reporter.Lines.Should().Equal(expected.OrderBy(c => c));
        }

        [Fact]
        public void GivenNuGetDeleteCommandItDisplaysCompletions()
        {
            var expected = new string[] {
                "--api-key",
                "--force-english-output",
                "--help",
                "--no-service-endpoint",
                "--non-interactive",
                "--source",
                "-h",
                "-k",
                "-s"
            };

            var reporter = new BufferedReporter();
            CompleteCommand.RunWithReporter(new[] { "dotnet nuget delete " }, reporter).Should().Be(0);
            reporter.Lines.Should().Equal(expected.OrderBy(c => c));
        }

        [Fact]
        public void GivenNuGetLocalsCommandItDisplaysCompletions()
        {
            var expected = new string[] {
                "--clear",
                "--force-english-output",
                "--help",
                "--list",
                "-c",
                "-h",
                "-l",
                "all",
                "global-packages",
                "http-cache",
                "temp"
            };

            var reporter = new BufferedReporter();
            CompleteCommand.RunWithReporter(new[] { "dotnet nuget locals " }, reporter).Should().Be(0);
            reporter.Lines.Should().Equal(expected.OrderBy(c => c));
        }

        [Fact]
        public void GivenNuGetPushCommandItDisplaysCompletions()
        {
            var expected = new string[] {
                "--api-key",
                "--disable-buffering",
                "--force-english-output",
                "--help",
                "--no-service-endpoint",
                "--no-symbols",
                "--source",
                "--symbol-api-key",
                "--symbol-source",
                "--timeout",
                "-d",
                "-h",
                "-k",
                "-n",
                "-s",
                "-sk",
                "-ss",
                "-t"
            };

            var reporter = new BufferedReporter();
            CompleteCommand.RunWithReporter(new[] { "dotnet nuget push " }, reporter).Should().Be(0);
            reporter.Lines.Should().Equal(expected.OrderBy(c => c));
        }
    }
}
