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
using Microsoft.DotNet.Tools.Install.Tool;
using Microsoft.DotNet.Tools.Test.Utilities.Mock;
using Microsoft.Extensions.DependencyModel.Tests;
using Microsoft.Extensions.EnvironmentAbstractions;
using Newtonsoft.Json;
using Xunit;
using Parser = Microsoft.DotNet.Cli.Parser;

namespace Microsoft.DotNet.Tests.ParserTests
{
    public class InstallToolCommandTests
    {
        private readonly IFileSystem _fileSystemWrapper;
        private readonly ToolPackageObtainerMock _toolPackageObtainerMock;
        private readonly ShellShimMakerMock _shellShimMakerMock;
        private readonly EnvironmentPathInstructionMock _environmentPathInstructionMock;
        private readonly AppliedOption _appliedCommand;
        private readonly ParseResult _parseResult;
        private readonly FakeReporter _fakeReporter;
        private const string PathToPlaceShim = "pathToPlace";

        public InstallToolCommandTests()
        {
            _fileSystemWrapper = new FileSystemMockBuilder().Build();
            _toolPackageObtainerMock = new ToolPackageObtainerMock(_fileSystemWrapper);
            _shellShimMakerMock = new ShellShimMakerMock(PathToPlaceShim, _fileSystemWrapper);
            _fakeReporter = new FakeReporter();
            _environmentPathInstructionMock =
                new EnvironmentPathInstructionMock(_fakeReporter, PathToPlaceShim);

            ParseResult result = Parser.Instance.Parse("dotnet install tool console.test.app");
            _appliedCommand = result["dotnet"]["install"]["tool"];
            var parser = Parser.Instance;
            _parseResult = parser.ParseFrom("dotnet install", new[] {"tool", "console.test.app"});
        }

        [Fact]
        public void WhenRunWithPackageIdItShouldCreateValidShim()
        {
            var installToolCommand = new InstallToolCommand(_appliedCommand,
                _parseResult,
                _toolPackageObtainerMock,
                _shellShimMakerMock,
                _environmentPathInstructionMock);

            installToolCommand.Execute();

            // It is hard to simulate shell behavior. Only Assert shim can point to executable dll
            _fileSystemWrapper.File.Exists(Path.Combine("pathToPlace", ToolPackageObtainerMock.FakeCommandName))
                .Should().BeTrue();
            var deserializedFakeShim = JsonConvert.DeserializeObject<ShellShimMakerMock.FakeShim>(
                _fileSystemWrapper.File.ReadAllText(
                    Path.Combine("pathToPlace",
                        ToolPackageObtainerMock.FakeCommandName)));
            _fileSystemWrapper.File.Exists(deserializedFakeShim.executablePath).Should().BeTrue();
        }

        [Fact]
        public void WhenRunWithPackageIdItShouldShowPathInstruction()
        {
            var installToolCommand = new InstallToolCommand(_appliedCommand,
                _parseResult,
                _toolPackageObtainerMock,
                _shellShimMakerMock,
                _environmentPathInstructionMock);

            installToolCommand.Execute();

            _fakeReporter.Message.Single().Should().NotBeEmpty();
        }

        [Fact]
        public void GivenFailedPackageObtainWhenRunWithPackageIdItShouldThrow()
        {
            var toolPackageObtainerSimulatorThatThrows
                = new ToolPackageObtainerMock(
                    _fileSystemWrapper,
                    () => throw new PackageObtainException("Simulated error"));
            var installToolCommand = new InstallToolCommand(
                _appliedCommand,
                _parseResult,
                toolPackageObtainerSimulatorThatThrows,
                _shellShimMakerMock,
                _environmentPathInstructionMock,
                _fakeReporter);

            Action a = () => installToolCommand.Execute();

            a.ShouldThrow<GracefulException>()
                .And.Message.Should()
                .Contain($"Install failed. Failed to download package:" +
                         $"{Environment.NewLine}NuGet returned:" +
                         $"{Environment.NewLine}" +
                         $"{Environment.NewLine}Simulated error");
        }

        [Fact]
        public void GivenInCorrectToolConfigurationWhenRunWithPackageIdItShouldThrow()
        {
            var toolPackageObtainerSimulatorThatThrows
                = new ToolPackageObtainerMock(
                    _fileSystemWrapper,
                    () => throw new ToolConfigurationException("Simulated error"));
            var installToolCommand = new InstallToolCommand(
                _appliedCommand,
                _parseResult,
                toolPackageObtainerSimulatorThatThrows,
                _shellShimMakerMock,
                _environmentPathInstructionMock,
                _fakeReporter);

            Action a = () => installToolCommand.Execute();
            a.ShouldThrow<GracefulException>()
                .And.Message.Should()
                .Contain($"Install failed. The settings file in the tool's NuGet package is not valid. Please contact the owner of the NuGet package." +
                         $"{Environment.NewLine}The error was:" +
                         $"{Environment.NewLine}" +
                         $"{Environment.NewLine}Simulated error");
        }

        [Fact]
        public void WhenRunWithPackageIdItShouldShowSuccessMessage()
        {
            var installToolCommand = new InstallToolCommand(
                _appliedCommand,
                _parseResult,
                _toolPackageObtainerMock,
                _shellShimMakerMock,
                new EnvironmentPathInstructionMock(_fakeReporter, PathToPlaceShim, true),
                _fakeReporter);

            installToolCommand.Execute();

            _fakeReporter
                .Message
                .Single().Should()
                .Contain(
                    "The installation succeeded. If there is no other instruction. You can type the following command in shell directly to invoke:");
        }

        internal class FakeReporter : IReporter
        {
            public List<string> Message { get; set; } = new List<string>();

            public void WriteLine(string message)
            {
                Message.Add(message);
            }

            public void WriteLine()
            {
                throw new NotImplementedException();
            }

            public void Write(string message)
            {
                throw new NotImplementedException();
            }
        }
    }
}
