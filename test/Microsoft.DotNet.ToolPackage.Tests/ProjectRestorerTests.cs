// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.Tools.Test.Utilities;
using Microsoft.DotNet.Tools.Tool.Install;
using Xunit;
using System.Linq;
using Microsoft.DotNet.Cli.CommandLine;
using Parser = Microsoft.DotNet.Cli.Parser;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.ToolPackage.Tests
{
    public class ProjectRestorerTests : TestBase
    {
        [Fact]
        void ItCreatesValidArgumentsToRestoreForwardingConfigPath()
        {
            const string configPath = "nuget.config";
            PackageLocation packageLocation = CreatePackageLocationByInstallCommand($"dotnet tool install -g console.test.app --configfile {configPath}");

            List<string> args = ProjectRestorer.ToRestoreArguments(packageLocation);

            AppliedOption restoreAppliedCommand = CreateRestoreCommandParseResultUsingToRestoreArguments(args);
            restoreAppliedCommand["--configfile"].Arguments.Single()
                .Should()
                .Be(new FilePath(configPath).Value);
        }

        [Fact]
        void ItCreatesValidArgumentsToRestoreForwardingDisableParallel()
        {
            PackageLocation packageLocation = CreatePackageLocationByInstallCommand($"dotnet tool install -g console.test.app --disable-parallel");

            List<string> args = ProjectRestorer.ToRestoreArguments(packageLocation);

            AppliedOption restoreAppliedCommand = CreateRestoreCommandParseResultUsingToRestoreArguments(args);
            restoreAppliedCommand.HasOption("--disable-parallel").Should().BeTrue();
        }

        [Fact]
        void ItCreatesValidArgumentsToRestoreForwardingDisableParallelWhenItIsMissing()
        {
            PackageLocation packageLocation = CreatePackageLocationByInstallCommand($"dotnet tool install -g console.test.app");

            List<string> args = ProjectRestorer.ToRestoreArguments(packageLocation);

            AppliedOption restoreAppliedCommand = CreateRestoreCommandParseResultUsingToRestoreArguments(args);
            restoreAppliedCommand.HasOption("--disable-parallel").Should().BeFalse();
        }

        [Fact]
        void ItCreatesValidArgumentsToRestoreForwardingNoCache()
        {
            PackageLocation packageLocation = CreatePackageLocationByInstallCommand($"dotnet tool install -g console.test.app --no-cache");

            List<string> args = ProjectRestorer.ToRestoreArguments(packageLocation);

            AppliedOption restoreAppliedCommand = CreateRestoreCommandParseResultUsingToRestoreArguments(args);
            restoreAppliedCommand.HasOption("--no-cache").Should().BeTrue();
        }

        [Fact]
        void ItCreatesValidArgumentsToRestoreForwardingIgnoreFailedSources()
        {
            PackageLocation packageLocation = CreatePackageLocationByInstallCommand($"dotnet tool install -g console.test.app --ignore-failed-sources");

            List<string> args = ProjectRestorer.ToRestoreArguments(packageLocation);

            AppliedOption restoreAppliedCommand = CreateRestoreCommandParseResultUsingToRestoreArguments(args);
            restoreAppliedCommand.HasOption("--ignore-failed-sources").Should().BeTrue();
        }

        [Fact]
        void ItCreatesValidArgumentsToRestoreForwardingMixArguments()
        {
            const string configPath = "nuget.config";
            PackageLocation packageLocation = CreatePackageLocationByInstallCommand($"dotnet tool install -g console.test.app --configfile {configPath} --ignore-failed-sources");

            List<string> args = ProjectRestorer.ToRestoreArguments(packageLocation);

            AppliedOption restoreAppliedCommand = CreateRestoreCommandParseResultUsingToRestoreArguments(args);
            restoreAppliedCommand.HasOption("--no-cache").Should().BeFalse();
            restoreAppliedCommand.HasOption("--disable-parallel").Should().BeFalse();
            restoreAppliedCommand.HasOption("--ignore-failed-sources").Should().BeTrue();
            restoreAppliedCommand["--configfile"].Arguments.Single()
                .Should()
                .Be(new FilePath(configPath).Value);
        }

        private static AppliedOption CreateRestoreCommandParseResultUsingToRestoreArguments(List<string> args)
        {
            args.Insert(0, "restore");
            args.Insert(0, "dotnet");

            AppliedOption appliedCommand = Parser.Instance.Parse(args.ToArray()).AppliedCommand();
            return appliedCommand;
        }

        private static PackageLocation CreatePackageLocationByInstallCommand(string installCommand)
        {
            var command = Parser.Instance;
            var result = command.Parse(installCommand);
            var parseResult = result["dotnet"]["tool"]["install"];
            var packageLocation = new PackageLocation(parseResult);
            return packageLocation;
        }
    }
}
