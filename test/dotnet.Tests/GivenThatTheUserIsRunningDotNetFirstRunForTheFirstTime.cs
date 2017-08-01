// Copyright (c) .NET Foundation and contributors. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.TestFramework;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;
using FluentAssertions;

namespace Microsoft.DotNet.Tests
{
    public class GivenThatTheUserIsRunningDotNetFirstRunForTheFirstTime : TestBase
    {
        private static CommandResult _firstDotnetNonVerbUseCommandResult;
        private static CommandResult _firstrunCommand;
        private static DirectoryInfo _nugetFallbackFolder;
        private static DirectoryInfo _dotDotnetFolder;
        private static string _testDirectory;

        static GivenThatTheUserIsRunningDotNetFirstRunForTheFirstTime()
        {
            _testDirectory = TestAssets.CreateTestDirectory("Dotnet_first_time_experience_tests").FullName;
            var testNuGetHome = Path.Combine(_testDirectory, "nuget_home");
            var cliTestFallbackFolder = Path.Combine(testNuGetHome, ".dotnet", "NuGetFallbackFolder");

            var command = new DotnetCommand()
                .WithWorkingDirectory(_testDirectory);
            command.Environment["HOME"] = testNuGetHome;
            command.Environment["USERPROFILE"] = testNuGetHome;
            command.Environment["APPDATA"] = testNuGetHome;
            command.Environment["DOTNET_CLI_TEST_FALLBACKFOLDER"] = cliTestFallbackFolder;
            command.Environment["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "";
            command.Environment["SkipInvalidConfigurations"] = "true";

            _firstrunCommand = command.ExecuteWithCapturedOutput("firstrun");

            _nugetFallbackFolder = new DirectoryInfo(cliTestFallbackFolder);
            _dotDotnetFolder = new DirectoryInfo(Path.Combine(testNuGetHome, ".dotnet"));
        }

        [Fact]
        public void UsingDotnetForTheFirstTimeSucceeds()
        {
            _firstrunCommand
                .Should()
                .Pass();
        }

        [Fact]
        public void ItShowsTheAppropriateMessageToTheUser()
        {
            _firstrunCommand.StdOut
                .Should()
                .ContainVisuallySameFragment(Configurer.LocalizableStrings.FirstTimeWelcomeMessage)
                .And.NotContain("Restore completed in");
        }

        [Fact]
        public void ItCreatesASentinelFileUnderTheNuGetCacheFolder()
        {
            _nugetFallbackFolder
                .Should()
                .HaveFile($"{GetDotnetVersion()}.dotnetSentinel");
        }

        [Fact]
        public void ItCreatesAFirstUseSentinelFileUnderTheDotDotNetFolder()
        {
            _dotDotnetFolder
                .Should()
                .HaveFile($"{GetDotnetVersion()}.dotnetFirstUseSentinel");
        }

        private string GetDotnetVersion()
        {
            return new DotnetCommand().ExecuteWithCapturedOutput("--version").StdOut
                .TrimEnd(Environment.NewLine.ToCharArray());
        }
    }
}
