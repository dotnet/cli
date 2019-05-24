// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using FluentAssertions;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;
using System.Collections.Generic;


namespace Microsoft.DotNet.InstallationScript.Tests
{
    public class GivenThatIWantToInstallTheSdkFromAScript : TestBase
    {

        [WindowsOnlyTheoryAttribute]
        [InlineData("-NoPath", "")]
        [InlineData("-Verbose", "")]
        [InlineData("-NoCdn", "")]
        [InlineData("-ProxyUseDefaultCredentials", "")]
        [InlineData("-AzureFeed", "https://dotnetcli.azureedge.net/dotnet")]
        [InlineData("-UncachedFeed", "https://dotnetcli.blob.core.windows.net/dotnet")]
        public void WhenVariousParametersArePassedToBashshellInstallScripts(string parameter, string value)
        {
            var args = new List<string> { "-dryrun", parameter };
            if (!string.IsNullOrEmpty(value))
            {
                args.Add(value);
            }

            var commandResult = CreateInstallCommand(args)
                            .CaptureStdOut()
                            .CaptureStdErr()
                            .Execute();

            Console.WriteLine(commandResult.StdOut);

            //  Standard criterium
            commandResult.Should().Pass();
            commandResult.Should().NotHaveStdOutContaining("dryrun");
            commandResult.Should().HaveStdOutContaining("Repeatable invocation:");

            //  Non-dynamic input parameters should always be on the ouput line
            commandResult.Should().HaveStdOutContainingIgnoreCase(parameter);

        }


        [NonWindowsOnlyTheoryAttribute]
        [InlineData("-no-path", "")]
        [InlineData("-verbose", "")]
        [InlineData("-no-cdn", "")]
        [InlineData("-azure-feed", "https://dotnetcli.azureedge.net/dotnet")]
        [InlineData("-uncached-feed", "https://dotnetcli.blob.core.windows.net/dotnet")]
        public void WhenVariousParametersArePassedToPowershellInstallScripts(string parameter, string value)
        {
            var args = new List<string> { "-dryrun", parameter };
            if (!string.IsNullOrEmpty(value))
            {
                args.Add(value);
            }

            var commandResult = CreateInstallCommand(args)
                            .CaptureStdOut()
                            .CaptureStdErr()
                            .Execute();

            Console.WriteLine(commandResult.StdOut);

            //  Standard criterium
            commandResult.Should().Pass();
            commandResult.Should().NotHaveStdOutContaining("dryrun");
            commandResult.Should().HaveStdOutContaining("Repeatable invocation:");

            //  Non-dynamic input parameters should always be on the ouput line
            commandResult.Should().HaveStdOutContainingIgnoreCase(parameter);

        }


        [Theory(Skip = "https://github.com/dotnet/cli/issues/2073")]
        [InlineData("1.0")]
        [InlineData("1.1")]
        [InlineData("2.0")]
        [InlineData("2.1")]
        [InlineData("Current")]
        [InlineData("LTS")]
        [InlineData("master")]
        [InlineData("release/1.0.0")]
        [InlineData("release/2.0.0")]
        [InlineData("release/2.0.2")]
        [InlineData("release/2.1.1xx")]
        [InlineData("release/2.1.2xx")]
        [InlineData("release/2.1.3xx")]
        [InlineData("release/2.1.4xx")]
        [InlineData("release/2.1.401")]
        [InlineData("release/2.1.5xx")]
        [InlineData("release/2.1.502")]
        [InlineData("release/2.1.6xx")]
        [InlineData("release/2.1.7xx")]
        [InlineData("release/2.1.8xx")]
        [InlineData("release/2.2.1xx")]
        [InlineData("release/2.2.2xx")]
        [InlineData("release/2.2.3xx")]
        [InlineData("release/2.2.4xx")]
        [InlineData("release/3.0.1xx")]
        public void WhenChannelResolvesToASpecificVersion(string channel)
        {
            var args = new string[] { "-dryrun", "-channel", channel };

            var commandResult = CreateInstallCommand(args)
                            .CaptureStdOut()
                            .CaptureStdErr()
                            .Execute();

            Console.WriteLine(commandResult.StdOut);

            //  Standard criterium
            commandResult.Should().Pass();
            commandResult.Should().NotHaveStdOutContaining("dryrun");
            commandResult.Should().HaveStdOutContaining("Repeatable invocation:");

            //  Channel should be translated to a specific version
            commandResult.Should().HaveStdOutContainingIgnoreCase("version");

        }


        private static Command CreateInstallCommand(IEnumerable<string> args)
        {
            var path = "";
            var finalArgs = "";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                path = "powershell.exe";
                finalArgs = "-ExecutionPolicy Bypass -NoProfile -NoLogo -Command \"" + Path.Combine(RepoDirectoriesProvider.RepoRoot, "scripts", "obtain", "dotnet-install.ps1") + " " + ArgumentEscaper.EscapeAndConcatenateArgArrayForProcessStart(args) + "\"";
            }
            else
            {
                path = Path.Combine(RepoDirectoriesProvider.RepoRoot, "scripts", "obtain", "dotnet-install.sh");
                finalArgs = ArgumentEscaper.EscapeAndConcatenateArgArrayForProcessStart(args);
            }

            var psi = new ProcessStartInfo
            {
                FileName = path,
                Arguments = finalArgs,
                UseShellExecute = false
            };

            var _process = new Process
            {
                StartInfo = psi
            };

            return new Command(_process);
        }

    }
}
