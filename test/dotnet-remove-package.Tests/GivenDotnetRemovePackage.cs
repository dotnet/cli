// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using FluentAssertions;
using Microsoft.DotNet.Tools;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;

namespace Microsoft.DotNet.Cli.Remove.Package.Tests
{
    public class GivenDotnetRemovePackage : TestBase
    {
        private const string HelpText = @".NET Remove Package reference Command.

Usage: dotnet remove <PROJECT> package [options] <PACKAGE_NAME>

Arguments:
  <PROJECT>        The project file to operate on. If a file is not specified, the command will search the current directory for one.
  <PACKAGE_NAME>   Package reference to remove.

Options:
  -h, --help   Show help information.
";

        private const string RemoveCommandHelpText = @".NET Remove Command

Usage: dotnet remove [options] <PROJECT> [command]

Arguments:
  <PROJECT>   The project file to operate on. If a file is not specified, the command will search the current directory for one.

Options:
  -h, --help   Show help information.

Commands:
  package <PACKAGE_NAME>   .NET Remove Package reference Command.
  reference <args>         .NET Remove Project to Project reference Command
";

        [Theory]
        [InlineData("--help")]
        [InlineData("-h")]
        public void WhenHelpOptionIsPassedItPrintsUsage(string helpArg)
        {
            var cmd = new DotnetCommand().ExecuteWithCapturedOutput($"remove package {helpArg}");
            cmd.Should().Pass();
            cmd.StdOut.Should().BeVisuallyEquivalentToIfNotLocalized(HelpText);
        }

        [Theory]
        [InlineData("")]
        [InlineData("unknownCommandName")]
        public void WhenNoCommandIsPassedItPrintsError(string commandName)
        {
            var cmd = new DotnetCommand()
                .ExecuteWithCapturedOutput($"remove {commandName}");
            cmd.Should().Fail();
            cmd.StdErr.Should().Be(CommonLocalizableStrings.RequiredCommandNotPassed);
            cmd.StdOut.Should().BeVisuallyEquivalentToIfNotLocalized(RemoveCommandHelpText);
        }

        [Fact]
        public void WhenReferencedPackageIsPassedItGetsRemoved()
        {
            var projectDirectory = TestAssets
                .Get("TestAppSimple")
                .CreateInstance()
                .WithSourceFiles()
                .Root
                .FullName;

            var packageName = "Newtonsoft.Json";
            var add = new DotnetCommand()
                .WithWorkingDirectory(projectDirectory)
                .ExecuteWithCapturedOutput($"add package {packageName}");
            add.Should().Pass();
          

            var remove = new DotnetCommand()
                .WithWorkingDirectory(projectDirectory)
                .ExecuteWithCapturedOutput($"remove package {packageName}");

            remove.Should().Pass();
            remove.StdOut.Should().Contain($"Removing PackageReference for package '{packageName}' from project '{projectDirectory + Path.DirectorySeparatorChar}TestAppSimple.csproj'.");
            remove.StdErr.Should().BeEmpty();
        }
    }
}
