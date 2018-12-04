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
using Microsoft.DotNet.Tools.Tool.List;
using Microsoft.DotNet.Tools.Test.Utilities;
using Microsoft.Extensions.DependencyModel.Tests;
using Microsoft.Extensions.EnvironmentAbstractions;
using Moq;
using NuGet.Versioning;
using Xunit;
using Parser = Microsoft.DotNet.Cli.Parser;
using LocalizableStrings = Microsoft.DotNet.Tools.Tool.List.LocalizableStrings;

namespace Microsoft.DotNet.Tests.Commands
{
    public class ToolListGlobalOrToolPathCommandTests
    {
        private readonly BufferedReporter _reporter;

        public ToolListGlobalOrToolPathCommandTests()
        {
            _reporter = new BufferedReporter();
        }

        [Fact]
        public void GivenAMissingGlobalOrToolPathOptionItErrors()
        {
            var store = new Mock<IToolPackageStoreQuery>(MockBehavior.Strict);

            var command = CreateCommand(store.Object);

            Action a = () => {
                command.Execute();
            };

            a.ShouldThrow<GracefulException>()
             .And
             .Message
             .Should()
             .Be(LocalizableStrings.NeedGlobalOrToolPath);
        }

        [Fact]
        public void GivenBothGlobalAndToolPathOptionsItErrors()
        {
            var store = new Mock<IToolPackageStoreQuery>(MockBehavior.Strict);

            var toolPath = Path.GetTempPath();
            var command = CreateCommand(store.Object, $"-g --tool-path {toolPath}", toolPath);

            Action a = () => {
                command.Execute();
            };

            a.ShouldThrow<GracefulException>()
             .And
             .Message
             .Should()
             .Be(LocalizableStrings.GlobalAndToolPathConflict);
        }

        [Fact]
        public void GivenNoInstalledPackagesItPrintsEmptyTable()
        {
            var store = new Mock<IToolPackageStoreQuery>(MockBehavior.Strict);
            store
                .Setup(s => s.EnumeratePackages())
                .Returns(new IToolPackage[0]);

            var command = CreateCommand(store.Object, "-g");

            command.Execute().Should().Be(0);

            _reporter.Lines.Should().Equal(EnumerateExpectedTableLines(store.Object));
        }

        [Fact]
        public void GivenAnInvalidToolPathItThrowsException()
        {
            var store = new Mock<IToolPackageStoreQuery>(MockBehavior.Strict);
            store
                .Setup(s => s.EnumeratePackages())
                .Returns(new IToolPackage[0]);

            var toolPath = "tool-path-does-not-exist";
            var command = CreateCommand(store.Object, $"--tool-path {toolPath}", toolPath);

            Action a = () => command.Execute();

            a.ShouldThrow<GracefulException>()
             .And
             .Message
             .Should()
             .Be(string.Format(LocalizableStrings.InvalidToolPathOption, toolPath));
        }

        [Fact]
        public void GivenAToolPathItPassesToolPathToStoreFactory()
        {
            var store = new Mock<IToolPackageStoreQuery>(MockBehavior.Strict);
            store
                .Setup(s => s.EnumeratePackages())
                .Returns(new IToolPackage[0]);

            var toolPath = Path.GetTempPath();
            var command = CreateCommand(store.Object, $"--tool-path {toolPath}", toolPath);

            command.Execute().Should().Be(0);

            _reporter.Lines.Should().Equal(EnumerateExpectedTableLines(store.Object));
        }

        [Fact]
        public void GivenASingleInstalledPackageItPrintsThePackage()
        {
            var store = new Mock<IToolPackageStoreQuery>(MockBehavior.Strict);
            store
                .Setup(s => s.EnumeratePackages())
                .Returns(new[] {
                    CreateMockToolPackage(
                        "test.tool",
                        "1.3.5-preview",
                        new[] {
                            new RestoredCommand(new ToolCommandName("foo"), "dotnet", new FilePath("tool"))
                        }
                    )
                });

            var command = CreateCommand(store.Object, "-g");

            command.Execute().Should().Be(0);

            _reporter.Lines.Should().Equal(EnumerateExpectedTableLines(store.Object));
        }

        [Fact]
        public void GivenMultipleInstalledPackagesItPrintsThePackages()
        {
            var store = new Mock<IToolPackageStoreQuery>(MockBehavior.Strict);
            store
                .Setup(s => s.EnumeratePackages())
                .Returns(new[] {
                    CreateMockToolPackage(
                        "test.tool",
                        "1.3.5-preview",
                        new[] {
                            new RestoredCommand(new ToolCommandName("foo"), "dotnet", new FilePath("tool"))
                        }
                    ),
                    CreateMockToolPackage(
                        "another.tool",
                        "2.7.3",
                        new[] {
                            new RestoredCommand(new ToolCommandName("bar"), "dotnet", new FilePath("tool"))
                        }
                    ),
                    CreateMockToolPackage(
                        "some.tool",
                        "1.0.0",
                        new[] {
                            new RestoredCommand(new ToolCommandName("fancy-foo"), "dotnet", new FilePath("tool"))
                        }
                    )
                });

            var command = CreateCommand(store.Object, "-g");

            command.Execute().Should().Be(0);

            _reporter.Lines.Should().Equal(EnumerateExpectedTableLines(store.Object));
        }

        [Fact]
        public void GivenAPackageWithMultipleCommandsItListsThem()
        {
            var store = new Mock<IToolPackageStoreQuery>(MockBehavior.Strict);
            store
                .Setup(s => s.EnumeratePackages())
                .Returns(new[] {
                    CreateMockToolPackage(
                        "test.tool",
                        "1.3.5-preview",
                        new[] {
                            new RestoredCommand(new ToolCommandName("foo"), "dotnet", new FilePath("tool")),
                            new RestoredCommand(new ToolCommandName("bar"), "dotnet", new FilePath("tool")),
                            new RestoredCommand(new ToolCommandName("baz"), "dotnet", new FilePath("tool"))
                        }
                    )
                });

            var command = CreateCommand(store.Object, "-g");

            command.Execute().Should().Be(0);

            _reporter.Lines.Should().Equal(EnumerateExpectedTableLines(store.Object));
        }

        [Fact]
        public void GivenABrokenPackageItPrintsWarning()
        {
            var store = new Mock<IToolPackageStoreQuery>(MockBehavior.Strict);
            store
                .Setup(s => s.EnumeratePackages())
                .Returns(new[] {
                    CreateMockToolPackage(
                        "test.tool",
                        "1.3.5-preview",
                        new[] {
                            new RestoredCommand(new ToolCommandName("foo"), "dotnet", new FilePath("tool"))
                        }
                    ),
                    CreateMockBrokenPackage("another.tool", "2.7.3"),
                    CreateMockToolPackage(
                        "some.tool",
                        "1.0.0",
                        new[] {
                            new RestoredCommand(new ToolCommandName("fancy-foo"), "dotnet", new FilePath("tool"))
                        }
                    )
                });

            var command = CreateCommand(store.Object, "-g");

            command.Execute().Should().Be(0);

            _reporter.Lines.Should().Equal(
                EnumerateExpectedTableLines(store.Object).Prepend(
                    string.Format(
                        LocalizableStrings.InvalidPackageWarning,
                        "another.tool",
                        "broken").Yellow()));
        }

        private IToolPackage CreateMockToolPackage(string id, string version, IReadOnlyList<RestoredCommand> commands)
        {
            var package = new Mock<IToolPackage>(MockBehavior.Strict);

            package.SetupGet(p => p.Id).Returns(new PackageId(id));
            package.SetupGet(p => p.Version).Returns(NuGetVersion.Parse(version));
            package.SetupGet(p => p.Commands).Returns(commands);
            return package.Object;
        }

        private IToolPackage CreateMockBrokenPackage(string id, string version)
        {
            var package = new Mock<IToolPackage>(MockBehavior.Strict);

            package.SetupGet(p => p.Id).Returns(new PackageId(id));
            package.SetupGet(p => p.Version).Returns(NuGetVersion.Parse(version));
            package.SetupGet(p => p.Commands).Throws(new ToolConfigurationException("broken"));
            return package.Object;
        }

        private ListToolGlobalOrToolPathCommand CreateCommand(IToolPackageStoreQuery store, string options = "", string expectedToolPath = null)
        {
            ParseResult result = Parser.Instance.Parse("dotnet tool list " + options);
            return new ListToolGlobalOrToolPathCommand(
                result["dotnet"]["tool"]["list"],
                result,
                toolPath => { AssertExpectedToolPath(toolPath, expectedToolPath); return store; },
                _reporter);
        }

        private void AssertExpectedToolPath(DirectoryPath? toolPath, string expectedToolPath)
        {
            if (expectedToolPath != null)
            {
                toolPath.Should().NotBeNull();
                toolPath.Value.Value.Should().Be(expectedToolPath);
            }
            else
            {
                toolPath.Should().BeNull();
            }
        }

        private IEnumerable<string> EnumerateExpectedTableLines(IToolPackageStoreQuery store)
        {
            string GetCommandsString(IToolPackage package)
            {
                return string.Join(ListToolGlobalOrToolPathCommand.CommandDelimiter, package.Commands.Select(c => c.Name));
            }

            var packages = store.EnumeratePackages().Where(PackageHasCommands).OrderBy(package => package.Id);
            var columnDelimiter = PrintableTable<IToolPackageStoreQuery>.ColumnDelimiter;

            int packageIdColumnWidth = LocalizableStrings.PackageIdColumn.Length;
            int versionColumnWidth = LocalizableStrings.VersionColumn.Length;
            int commandsColumnWidth = LocalizableStrings.CommandsColumn.Length;
            foreach (var package in packages)
            {
                packageIdColumnWidth = Math.Max(packageIdColumnWidth, package.Id.ToString().Length);
                versionColumnWidth = Math.Max(versionColumnWidth, package.Version.ToNormalizedString().Length);
                commandsColumnWidth = Math.Max(commandsColumnWidth, GetCommandsString(package).Length);
            }

            yield return string.Format(
                "{0}{1}{2}{3}{4}",
                LocalizableStrings.PackageIdColumn.PadRight(packageIdColumnWidth),
                columnDelimiter,
                LocalizableStrings.VersionColumn.PadRight(versionColumnWidth),
                columnDelimiter,
                LocalizableStrings.CommandsColumn.PadRight(commandsColumnWidth));

            yield return new string(
                '-',
                packageIdColumnWidth + versionColumnWidth + commandsColumnWidth + (columnDelimiter.Length * 2));

            foreach (var package in packages)
            {
                yield return string.Format(
                    "{0}{1}{2}{3}{4}",
                    package.Id.ToString().PadRight(packageIdColumnWidth),
                    columnDelimiter,
                    package.Version.ToNormalizedString().PadRight(versionColumnWidth),
                    columnDelimiter,
                    GetCommandsString(package).PadRight(commandsColumnWidth));
            }
        }

        private static bool PackageHasCommands(IToolPackage package)
        {
            try
            {
                return package.Commands.Count >= 0;
            }
            catch (Exception ex) when (ex is ToolConfigurationException)
            {
                return false;
            }
        }
    }
}
