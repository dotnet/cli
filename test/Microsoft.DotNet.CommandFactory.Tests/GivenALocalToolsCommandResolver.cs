// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.CommandFactory;
using Microsoft.DotNet.ToolManifest;
using Microsoft.DotNet.ToolPackage;
using Microsoft.DotNet.Tools.Test.Utilities;
using Microsoft.DotNet.Tools.Tests.ComponentMocks;
using Microsoft.DotNet.Tools.Tests.Utilities;
using Microsoft.Extensions.DependencyModel.Tests;
using Microsoft.Extensions.EnvironmentAbstractions;
using NuGet.Frameworks;
using NuGet.Versioning;
using Xunit;

namespace Microsoft.DotNet.Tests
{
    public class GivenALocalToolsCommandResolver : TestBase
    {
        private const string ManifestFilename = "dotnet-tools.json";
        private readonly string _testDirectoryRoot;
        private DirectoryPath _nugetGlobalPackagesFolder;
        private readonly LocalToolsResolverCache _localToolsResolverCache;
        private readonly IFileSystem _fileSystem;

        public GivenALocalToolsCommandResolver()
        {
            _fileSystem = new FileSystemMockBuilder().UseCurrentSystemTemporaryDirectory().Build();
            _nugetGlobalPackagesFolder = new DirectoryPath(NuGetGlobalPackagesFolder.GetLocation());
            string temporaryDirectory = _fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;
            _testDirectoryRoot = _fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;
            _localToolsResolverCache = new LocalToolsResolverCache(
                _fileSystem,
                new DirectoryPath(Path.Combine(temporaryDirectory, "cache")));
        }

        [Fact]
        public void ItCanFindToolExecutable()
        {
            var toolCommand = "a";
            NuGetVersion packageVersionA = NuGetVersion.Parse("1.0.4");
            _fileSystem.File.WriteAllText(Path.Combine(_testDirectoryRoot, ManifestFilename),
                _jsonContent.Replace("$TOOLCOMMAND$", toolCommand));
            ToolManifestFinder toolManifest =
                new ToolManifestFinder(new DirectoryPath(_testDirectoryRoot), _fileSystem);
            ToolCommandName toolCommandNameA = new ToolCommandName(toolCommand);
            var fakeExecutable = _nugetGlobalPackagesFolder.WithFile("fakeExecutable.dll");
            _fileSystem.Directory.CreateDirectory(_nugetGlobalPackagesFolder.Value);
            _fileSystem.File.CreateEmptyFile(fakeExecutable.Value);
            _localToolsResolverCache.Save(
                new Dictionary<RestoredCommandIdentifier, RestoredCommand>
                {
                    [new RestoredCommandIdentifier(
                            new PackageId("local.tool.console.a"),
                            packageVersionA,
                            NuGetFramework.Parse(BundledTargetFramework.GetTargetFrameworkMoniker()),
                            Constants.AnyRid,
                            toolCommandNameA)]
                        = new RestoredCommand(toolCommandNameA, "dotnet", fakeExecutable)
                });

            var localToolsCommandResolver = new LocalToolsCommandResolver(
                toolManifest,
                _localToolsResolverCache,
                _fileSystem);

            var result = localToolsCommandResolver.Resolve(new CommandResolverArguments()
            {
                CommandName = "dotnet-a",
            });

            result.Should().NotBeNull();

            var commandPath = result.Args.Trim('"');
            _fileSystem.File.Exists(commandPath).Should().BeTrue("the following path exists: " + commandPath);
            commandPath.Should().Be(fakeExecutable.Value);
        }

        [Fact]
        public void WhenNuGetGlobalPackageLocationIsCleanedAfterRestoreItShowError()
        {
            ToolCommandName toolCommandNameA = new ToolCommandName("a");
            NuGetVersion packageVersionA = NuGetVersion.Parse("1.0.4");
            _fileSystem.File.WriteAllText(Path.Combine(_testDirectoryRoot, ManifestFilename),
                _jsonContent.Replace("$TOOLCOMMAND$", toolCommandNameA.Value));
            ToolManifestFinder toolManifest =
                new ToolManifestFinder(new DirectoryPath(_testDirectoryRoot), _fileSystem);

            var fakeExecutable = _nugetGlobalPackagesFolder.WithFile("fakeExecutable.dll");
            _fileSystem.Directory.CreateDirectory(_nugetGlobalPackagesFolder.Value);
            _fileSystem.File.CreateEmptyFile(fakeExecutable.Value);
            _localToolsResolverCache.Save(
                new Dictionary<RestoredCommandIdentifier, RestoredCommand>
                {
                    [new RestoredCommandIdentifier(
                            new PackageId("local.tool.console.a"),
                            packageVersionA,
                            NuGetFramework.Parse(BundledTargetFramework.GetTargetFrameworkMoniker()),
                            Constants.AnyRid,
                            toolCommandNameA)]
                        = new RestoredCommand(toolCommandNameA, "dotnet", fakeExecutable)
                });

            var localToolsCommandResolver = new LocalToolsCommandResolver(
                toolManifest,
                _localToolsResolverCache,
                _fileSystem);

            _fileSystem.File.Delete(fakeExecutable.Value);

            Action action = () => localToolsCommandResolver.Resolve(new CommandResolverArguments()
            {
                CommandName = $"dotnet-{toolCommandNameA.ToString()}",
            });

            action.ShouldThrow<GracefulException>(string.Format(CommandFactory.LocalizableStrings.NeedRunToolRestore,
                toolCommandNameA.ToString()));
        }

        private string _jsonContent =
            @"{
   ""version"":1,
   ""isRoot"":true,
   ""tools"":{
      ""local.tool.console.a"":{
         ""version"":""1.0.4"",
         ""commands"":[
            ""$TOOLCOMMAND$""
         ]
      }
   }
}";
    }
}
