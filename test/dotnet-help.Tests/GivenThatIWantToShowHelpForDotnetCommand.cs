// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Construction;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;
using FluentAssertions;

namespace Microsoft.DotNet.Help.Tests
{
    public class GivenThatIWantToShowHelpForDotnetCommand : TestBase
    {
        private const string HelpText =
@"Usage: dotnet [host-options] [command] [arguments] [common-options]

Arguments:
  [command]             The command to execute
  [arguments]           Arguments to pass to the command
  [host-options]        Options specific to dotnet (host)
  [common-options]      Options common to all commands

Common options:
  -v|--verbose          Enable verbose output
  -h|--help             Show help 

Host options (passed before the command):
  -d|--diagnostics      Enable diagnostic output
  --version             Display .NET CLI Version Number
  --info                Display .NET CLI Info

Commands:
  new           Initialize .NET projects.
  restore       Restore dependencies specified in the .NET project.
  build         Builds a .NET project.
  publish       Publishes a .NET project for deployment (including the runtime).
  run           Compiles and immediately executes a .NET project.
  test          Runs unit tests using the test runner specified in the project.
  pack          Creates a NuGet package.
  migrate       Migrates a project.json based project to a msbuild based project.
  clean         Clean build output(s).
  sln           Modify solution (SLN) files.

Project modification commands:
  add           Add items to the project
  remove        Remove items from the project
  list          List items in the project

Advanced Commands:
  nuget         Provides additional NuGet commands.
  msbuild       Runs Microsoft Build Engine (MSBuild).
  vstest        Runs Microsoft Test Execution Command Line Tool.";

        [Theory]
        [InlineData("--help")]
        [InlineData("-h")]
        [InlineData("-?")]
        [InlineData("/?")]
        public void WhenHelpOptionIsPassedToDotnetItPrintsUsage(string helpArg)
        {
            var cmd = new DotnetCommand()
                .ExecuteWithCapturedOutput($"{helpArg}");
            cmd.Should().Pass();
            cmd.StdOut.Should().ContainVisuallySameFragment(HelpText);
        }

        [Theory]
        [InlineData("pack", "Usage: dotnet pack [arguments] [options] [args]")]
        [InlineData("new", "Usage: dotnet new [arguments] [options]")]
        [InlineData("restore", "Usage: dotnet restore [arguments] [options] [args]")]
        [InlineData("build", "Usage: dotnet build [arguments] [options] [args]")]
        [InlineData("publish", "Usage: dotnet publish [arguments] [options] [args]")]
        [InlineData("run", "Usage: dotnet run [options] [[--] <additional arguments>...]]")]
        [InlineData("test", "Usage: dotnet test [arguments] [options] [args]")]
        [InlineData("migrate", "Usage: dotnet migrate [arguments] [options]")]
        [InlineData("clean", "Usage: dotnet clean [arguments] [options] [args]")]
        [InlineData("sln", "Usage: dotnet sln [arguments] [options] [command]")]
        [InlineData("sln add", "Usage: dotnet sln <SLN_FILE> add [options] [args]")]
        [InlineData("sln list", "Usage: dotnet sln <SLN_FILE> list [options]")]
        [InlineData("sln remove", "Usage: dotnet sln <SLN_FILE> remove [options] [args]")]
        [InlineData("nuget", "Usage: dotnet nuget [options] [command]")]
        // [InlineData("msbuild", "")]
        // [InlineData("vstest", "")]
        public void HelpOptionUsageSummaryIsConsistent(string command, string expectedUsage)
        {
            var cmd = new DotnetCommand()
                .Execute($"{command} -h");
            
            cmd.Should().Pass();
            
            var actualUsage = cmd.StdOut.Split('\n').ElementAt(2);
            actualUsage.Should().Be(expectedUsage);
        }    
    }
}
