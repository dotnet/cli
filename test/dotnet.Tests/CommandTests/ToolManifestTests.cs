// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Microsoft.DotNet.ToolManifest;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ToolPackage;
using Microsoft.DotNet.Tools.Test.Utilities;
using Microsoft.Extensions.DependencyModel.Tests;
using Microsoft.Extensions.EnvironmentAbstractions;
using NuGet.Frameworks;
using NuGet.Versioning;
using Xunit;
using LocalizableStrings = Microsoft.DotNet.ToolManifest.LocalizableStrings;

namespace Microsoft.DotNet.Tests.Commands
{
    public class ToolManifestTests
    {
        private readonly IFileSystem _fileSystem;

        public ToolManifestTests()
        {
            _fileSystem = new FileSystemMockBuilder().UseCurrentSystemTemporaryDirectory().Build();
            _testDirectoryRoot = _fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;

            _defaultExpectedResult = new List<ToolManifestFindingResultSinglePackage>
            {
                new ToolManifestFindingResultSinglePackage(
                    new PackageId("t-rex"),
                    NuGetVersion.Parse("1.0.53"),
                    new[] {new ToolCommandName("t-rex")},
                    NuGetFramework.Parse("netcoreapp2.1")),
                new ToolManifestFindingResultSinglePackage(
                    new PackageId("dotnetsay"),
                    NuGetVersion.Parse("2.1.4"),
                    new[] {new ToolCommandName("dotnetsay")})
            };
        }

        [Fact]
        public void GivenManifestFileOnSameDirectoryItGetContent()
        {
            _fileSystem.File.WriteAllText(Path.Combine(_testDirectoryRoot, _manifestFilename), _jsonContent);
            var toolManifest = new ToolManifestFinder(new DirectoryPath(_testDirectoryRoot), _fileSystem);
            var manifestResult = toolManifest.Find();

            manifestResult.ShouldBeEquivalentTo(_defaultExpectedResult);
        }

        [Fact]
        public void GivenManifestFileOnParentDirectoryItGetContent()
        {
            var subdirectoryOfTestRoot = Path.Combine(_testDirectoryRoot, "sub");
            _fileSystem.File.WriteAllText(Path.Combine(_testDirectoryRoot, _manifestFilename), _jsonContent);
            var toolManifest = new ToolManifestFinder(new DirectoryPath(subdirectoryOfTestRoot), _fileSystem);
            var manifestResult = toolManifest.Find();

            manifestResult.ShouldBeEquivalentTo(_defaultExpectedResult);
        }

        [Fact]
        // https://github.com/JamesNK/Newtonsoft.Json/issues/931#issuecomment-224104005
        // Due to a limitation of newtonsoft json
        public void GivenManifestWithDuplicatedPackageIdItReturnsTheLastValue()
        {
            _fileSystem.File.WriteAllText(Path.Combine(_testDirectoryRoot, _manifestFilename),
                _jsonWithDuplicatedPackagedId);
            var toolManifest = new ToolManifestFinder(new DirectoryPath(_testDirectoryRoot), _fileSystem);
            var manifestResult = toolManifest.Find();

            manifestResult.Should()
                .Contain(
                    new ToolManifestFindingResultSinglePackage(
                        new PackageId("t-rex"),
                        NuGetVersion.Parse("2.1.4"),
                        new[] { new ToolCommandName("t-rex") }));
        }

        [Fact]
        public void WhenCalledWithFilePathItGetContent()
        {
            string customFileName = "customname.file";
            _fileSystem.File.WriteAllText(Path.Combine(_testDirectoryRoot, customFileName), _jsonContent);
            var toolManifest = new ToolManifestFinder(new DirectoryPath(_testDirectoryRoot), _fileSystem);
            var manifestResult =
                toolManifest.Find(new FilePath(Path.Combine(_testDirectoryRoot, customFileName)));

            manifestResult.ShouldBeEquivalentTo(_defaultExpectedResult);
        }

        [Fact]
        public void WhenCalledWithNonExistsFilePathItThrows()
        {
            var toolManifest = new ToolManifestFinder(new DirectoryPath(_testDirectoryRoot), _fileSystem);
            Action a = () => toolManifest.Find(new FilePath(Path.Combine(_testDirectoryRoot, "non-exits")));
            a.ShouldThrow<ToolManifestException>().And.Message.Should().Contain(string.Format(LocalizableStrings.CannotFindAnyManifestsFileSearched, ""));
        }

        [Fact]
        public void GivenNoManifestFileItThrows()
        {
            var toolManifest = new ToolManifestFinder(new DirectoryPath(_testDirectoryRoot), _fileSystem);
            Action a = () => toolManifest.Find();
            a.ShouldThrow<ToolManifestException>().And.Message.Should().Contain(string.Format(LocalizableStrings.CannotFindAnyManifestsFileSearched, ""));
        }

        [Fact]
        public void GivenMissingFieldManifestFileItReturnError()
        {
            _fileSystem.File.WriteAllText(Path.Combine(_testDirectoryRoot, _manifestFilename), _jsonWithMissingField);
            var toolManifest = new ToolManifestFinder(new DirectoryPath(_testDirectoryRoot), _fileSystem);
            Action a = () => toolManifest.Find();

            a.ShouldThrow<ToolManifestException>().And.Message.Should().Contain(
                string.Format(LocalizableStrings.InvalidManifestFilePrefix,
                            string.Join(" ",
                                string.Format(LocalizableStrings.PackageNameAndErrors, "t-rex",
                                    LocalizableStrings.MissingVersion + ", " + LocalizableStrings.FieldCommandsIsMissing))));
        }

        [Fact]
        public void GivenInvalidFieldsManifestFileItReturnError()
        {
            _fileSystem.File.WriteAllText(Path.Combine(_testDirectoryRoot, _manifestFilename), _jsonWithInvalidField);
            var toolManifest = new ToolManifestFinder(new DirectoryPath(_testDirectoryRoot), _fileSystem);
            Action a = () => toolManifest.Find();

            a.ShouldThrow<ToolManifestException>().And.Message.Should().Contain(string.Format(LocalizableStrings.VersionIsInvalid, "1.*"));
            a.ShouldThrow<ToolManifestException>().And.Message.Should().Contain(string.Format(LocalizableStrings.TargetFrameworkIsUnsupported, "abc"));
        }

        // Remove this test when the follow pending test is enabled and feature is implemented.
        [Fact]
        public void RequireRootAndVersionIs1()
        {
            _fileSystem.File.WriteAllText(Path.Combine(_testDirectoryRoot, _manifestFilename), _jsonWithNonRoot);
            var toolManifest = new ToolManifestFinder(new DirectoryPath(_testDirectoryRoot), _fileSystem);
            Action a = () => toolManifest.Find();

            a.ShouldThrow<ToolManifestException>()
                .And.Message.Should()
                .Contain(LocalizableStrings.IsRootFalseNotSupported + " " + LocalizableStrings.Version1NotSupported);
        }

        [Fact(Skip = "pending implementation")]
        public void GivenConflictedManifestFileInDifferentFieldsItReturnMergedContent()
        {
        }

        [Fact(Skip = "pending implementation")]
        public void DifferentVersionOfManifestFileItShouldHaveWarnings()
        {
        }

        [Fact(Skip = "pending implementation")]
        public void DifferentVersionOfManifestFileItShouldNotThrow()
        {
        }

        private string _jsonContent =
            "{\"version\":1,\"isRoot\":true,\"tools\":{\"t-rex\":{\"version\":\"1.0.53\",\"commands\":[\"t-rex\"],\"targetFramework\":\"netcoreapp2.1\"},\"dotnetsay\":{\"version\":\"2.1.4\",\"commands\":[\"dotnetsay\"]}}}";

        private string _jsonWithDuplicatedPackagedId =
            "{\"version\":1,\"isRoot\":true,\"tools\":{\"t-rex\":{\"version\":\"1.0.53\",\"commands\":[\"t-rex\"],\"targetFramework\":\"netcoreapp2.1\"},\"t-rex\":{\"version\":\"2.1.4\",\"commands\":[\"t-rex\"]}}}";

        private string _jsonWithMissingField =
            "{\"version\":1,\"isRoot\":true,\"tools\":{\"t-rex\":{\"extra\":1}}}";

        private string _jsonWithInvalidField =
            "{\"version\":1,\"isRoot\":true,\"tools\":{\"t-rex\":{\"version\":\"1.*\",\"commands\":[\"t-rex\"],\"targetFramework\":\"abc\"}}}";

        private string _jsonWithNonRoot =
            "{\"version\":2,\"isRoot\":false,\"tools\":{\"t-rex\":{\"version\":\"1.0.53\",\"commands\":[\"t-rex\"]}}}";

        private readonly List<ToolManifestFindingResultSinglePackage> _defaultExpectedResult;
        private readonly string _testDirectoryRoot;
        private const string _manifestFilename = "localtool.manifest.json";
    }
}
