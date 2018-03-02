// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.CommandLine;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ToolPackage;
using Microsoft.DotNet.Tools;
using Microsoft.DotNet.Tools.Install.Tool;
using Microsoft.DotNet.Tools.Tests.ComponentMocks;
using Microsoft.DotNet.Tools.Test.Utilities;
using Microsoft.Extensions.DependencyModel.Tests;
using Microsoft.Extensions.EnvironmentAbstractions;
using Newtonsoft.Json;
using Xunit;
using Parser = Microsoft.DotNet.Cli.Parser;
using System.Runtime.InteropServices;
using LocalizableStrings = Microsoft.DotNet.Tools.Install.Tool.LocalizableStrings;

namespace Microsoft.DotNet.Tests.Commands
{
    public class InstallToolCommandTests
    {
        private readonly IFileSystem _fileSystem;
        private readonly IToolPackageStore _toolPackageStore;
        private readonly ShellShimRepositoryMock _shellShimRepositoryMock;
        private readonly EnvironmentPathInstructionMock _environmentPathInstructionMock;
        private readonly AppliedOption _appliedCommand;
        private readonly ParseResult _parseResult;
        private readonly BufferedReporter _reporter;
        private PassThroughToolPackageFactory _toolPackageFactory;
        private const string PathToPlaceShim = "pathToPlace";
        private const string PathToPlacePackages = PathToPlaceShim + "pkg";
        private const string PackageId = "global.tool.console.demo";
        private const string PackageVersion = "1.0.4";

        public InstallToolCommandTests()
        {
            _reporter = new BufferedReporter();
            _fileSystem = new FileSystemMockBuilder().Build();
            _toolPackageStore = new ToolPackageStoreMock(new DirectoryPath(PathToPlacePackages), _fileSystem);
            _shellShimRepositoryMock = new ShellShimRepositoryMock(new DirectoryPath(PathToPlaceShim), _fileSystem);
            _environmentPathInstructionMock =
                new EnvironmentPathInstructionMock(_reporter, PathToPlaceShim);

            ParseResult result = Parser.Instance.Parse($"dotnet install tool -g {PackageId}");
            _appliedCommand = result["dotnet"]["install"]["tool"];
            var parser = Parser.Instance;
            _parseResult = parser.ParseFrom("dotnet install", new[] {"tool", PackageId});
            _toolPackageFactory = new PassThroughToolPackageFactory(_toolPackageStore, CreateToolPackageInstaller());
        }

        [Fact]
        public void WhenRunWithPackageIdItShouldCreateValidShim()
        {
            var installToolCommand = new InstallToolCommand(_appliedCommand,
                _parseResult,
                _toolPackageFactory,
                _shellShimRepositoryMock,
                _environmentPathInstructionMock,
                _reporter);

            installToolCommand.Execute().Should().Be(0);

            // It is hard to simulate shell behavior. Only Assert shim can point to executable dll
            _fileSystem.File.Exists(ExpectedCommandPath()).Should().BeTrue();
            var deserializedFakeShim = JsonConvert.DeserializeObject<ShellShimRepositoryMock.FakeShim>(
                _fileSystem.File.ReadAllText(ExpectedCommandPath()));

            _fileSystem.File.Exists(deserializedFakeShim.ExecutablePath).Should().BeTrue();
        }

        [Fact]
        public void WhenRunWithPackageIdWithSourceItShouldCreateValidShim()
        {
            const string sourcePath = "http://mysouce.com";
            ParseResult result = Parser.Instance.Parse($"dotnet install tool -g {PackageId} --source {sourcePath}");
            AppliedOption appliedCommand = result["dotnet"]["install"]["tool"];
            ParseResult parseResult =
                Parser.Instance.ParseFrom("dotnet install", new[] { "tool", PackageId, "--source", sourcePath });

            var toolPackageFactory = new PassThroughToolPackageFactory(_toolPackageStore, CreateToolPackageInstaller(
                feeds: new MockFeed[] {
                    new MockFeed
                    {
                        Type = MockFeedType.Source,
                        Uri = sourcePath,
                        Packages = new List<MockFeedPackage>
                        {
                            new MockFeedPackage
                            {
                                PackageId = PackageId,
                                Version = PackageVersion
                            }
                        }
                    }
                }));

            var installToolCommand = new InstallToolCommand(appliedCommand,
                parseResult,
                toolPackageFactory,
                _shellShimRepositoryMock,
                _environmentPathInstructionMock,
                _reporter);

            installToolCommand.Execute().Should().Be(0);

            // It is hard to simulate shell behavior. Only Assert shim can point to executable dll
            _fileSystem.File.Exists(ExpectedCommandPath())
            .Should().BeTrue();
            var deserializedFakeShim =
                JsonConvert.DeserializeObject<ShellShimRepositoryMock.FakeShim>(
                    _fileSystem.File.ReadAllText(ExpectedCommandPath()));
            _fileSystem.File.Exists(deserializedFakeShim.ExecutablePath).Should().BeTrue();
        }

        [Fact]
        public void WhenRunWithPackageIdItShouldShowPathInstruction()
        {
            var installToolCommand = new InstallToolCommand(_appliedCommand,
                _parseResult,
                _toolPackageFactory,
                _shellShimRepositoryMock,
                _environmentPathInstructionMock,
                _reporter);

            installToolCommand.Execute().Should().Be(0);

            _reporter.Lines.First().Should().Be(EnvironmentPathInstructionMock.MockIstructionText);
        }

        [Fact]
        public void GivenFailedPackageInstallWhenRunWithPackageIdItShouldFail()
        {
            var toolPackageFactory = new PassThroughToolPackageFactory(
                _toolPackageStore,
                CreateToolPackageInstaller(
                    installCallback: () => throw new ToolPackageException("Simulated error")));

            var installToolCommand = new InstallToolCommand(
                _appliedCommand,
                _parseResult,
                toolPackageFactory,
                _shellShimRepositoryMock,
                _environmentPathInstructionMock,
                _reporter);

            installToolCommand.Execute().Should().Be(1);

            _reporter.Lines.Count.Should().Be(2);

            _reporter
                .Lines[0]
                .Should()
                .Contain("Simulated error");

            _reporter
                .Lines[1]
                .Should()
                .Contain(string.Format(LocalizableStrings.ToolInstallationFailed, PackageId));

            _fileSystem.Directory.Exists(Path.Combine(PathToPlacePackages, PackageId)).Should().BeFalse();
        }

        [Fact]
        public void GivenCreateShimItShouldHaveNoBrokenFolderOnDisk()
        {
            _fileSystem.File.CreateEmptyFile(ExpectedCommandPath()); // Create conflict shim

            var installToolCommand = new InstallToolCommand(
                _appliedCommand,
                _parseResult,
                _toolPackageFactory,
                _shellShimRepositoryMock,
                _environmentPathInstructionMock,
                _reporter);

            installToolCommand.Execute().Should().Be(1);

            _reporter
                .Lines[0]
                .Should()
                .Contain(
                    string.Format(
                        CommonLocalizableStrings.ShellShimConflict,
                        ProjectRestorerMock.FakeCommandName));

            _fileSystem.Directory.Exists(Path.Combine(PathToPlacePackages, PackageId)).Should().BeFalse();
        }

        [Fact]
        public void GivenInCorrectToolConfigurationWhenRunWithPackageIdItShouldFail()
        {
            var toolPackageFactory = new PassThroughToolPackageFactory(
                _toolPackageStore,
                CreateToolPackageInstaller(
                    installCallback: () => throw new ToolConfigurationException("Simulated error")));

            var installToolCommand = new InstallToolCommand(
                _appliedCommand,
                _parseResult,
                toolPackageFactory,
                _shellShimRepositoryMock,
                _environmentPathInstructionMock,
                _reporter);

            installToolCommand.Execute().Should().Be(1);

            _reporter.Lines.Count.Should().Be(2);

            _reporter
                .Lines[0]
                .Should()
                .Contain(
                    string.Format(
                        LocalizableStrings.InvalidToolConfiguration,
                        "Simulated error"));

            _reporter
                .Lines[1]
                .Should()
                .Contain(string.Format(LocalizableStrings.ToolInstallationFailedContactAuthor, PackageId));
        }

        [Fact]
        public void WhenRunWithPackageIdItShouldShowSuccessMessage()
        {
            var installToolCommand = new InstallToolCommand(
                _appliedCommand,
                _parseResult,
                _toolPackageFactory,
                _shellShimRepositoryMock,
                new EnvironmentPathInstructionMock(_reporter, PathToPlaceShim, true),
                _reporter);

            installToolCommand.Execute().Should().Be(0);

            _reporter
                .Lines
                .Single()
                .Should()
                .Contain(string.Format(
                    LocalizableStrings.InstallationSucceeded,
                    ProjectRestorerMock.FakeCommandName,
                    PackageId,
                    PackageVersion));
        }

        [Fact]
        public void WhenRunWithBothGlobalAndToolPathShowErrorMessage()
        {
            var result = Parser.Instance.Parse($"dotnet install tool -g --tool-path /tmp/folder {PackageId}");
            var appliedCommand = result["dotnet"]["install"]["tool"];
            var parser = Parser.Instance;
            var parseResult = parser.ParseFrom("dotnet install", new[] {"tool", PackageId});

            var installToolCommand = new InstallToolCommand(
                appliedCommand,
                parseResult,
                _toolPackageFactory,
                _shellShimRepositoryMock,
                new EnvironmentPathInstructionMock(_reporter, PathToPlaceShim, true),
                _reporter);

            Action a = () => installToolCommand.Execute();

            a.ShouldThrow<GracefulException>().And.Message
                .Should().Contain("Cannot have global and tool-path as opinion at the same time.");
        }

        [Fact]
        public void WhenRunWithPackageIdAndBinPathItShouldNoteHaveEnvironmentPathInstruction()
        {
            var result = Parser.Instance.Parse($"dotnet install tool --tool-path /tmp/folder {PackageId}");
            var appliedCommand = result["dotnet"]["install"]["tool"];
            var parser = Parser.Instance;
            var parseResult = parser.ParseFrom("dotnet install", new[] {"tool", PackageId});

            var installToolCommand = new InstallToolCommand(appliedCommand,
                parseResult,
                _toolPackageFactory,
                _shellShimRepositoryMock,
                new EnvironmentPathInstructionMock(_reporter, PathToPlaceShim),
                _reporter);

            installToolCommand.Execute().Should().Be(0);

            _reporter.Lines.Should().NotContain(l => l.Contains(EnvironmentPathInstructionMock.MockIstructionText));
        }

        private IToolPackageInstaller CreateToolPackageInstaller(
            IEnumerable<MockFeed> feeds = null,
            Action installCallback = null)
        {
            return new ToolPackageInstallerMock(
                fileSystem: _fileSystem,
                store: _toolPackageStore,
                projectRestorer: new ProjectRestorerMock(
                    fileSystem: _fileSystem,
                    reporter: _reporter,
                    feeds: feeds),
                installCallback: installCallback);
        }

        private static string ExpectedCommandPath()
        {
            var extension = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : string.Empty;
            return Path.Combine(
                "pathToPlace",
                ProjectRestorerMock.FakeCommandName + extension);
        }
    }
}
