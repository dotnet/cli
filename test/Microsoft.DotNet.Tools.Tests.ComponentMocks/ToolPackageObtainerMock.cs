// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.ToolPackage;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.Tools.Tests.ComponentMocks
{
    internal class ToolPackageObtainerMock : IToolPackageObtainer
    {
        public const string FakeEntrypointName = "SimulatorEntryPoint.dll";
        public const string FakeCommandName = "SimulatorCommand";
        private readonly Action _beforeRunObtain;
        private static IFileSystem _fileSystem;
        private string _fakeExecutableDirectory;
        private List<MockFeed> _mockFeeds;
        private string _packageIdVersionDirectory;

        public ToolPackageObtainerMock(
            IFileSystem fileSystemWrapper = null,
            bool useDefaultFeed = true,
            IEnumerable<MockFeed> additionalFeeds = null,
            Action beforeRunObtain = null)
        {
            _beforeRunObtain = beforeRunObtain ?? (() => { });
            _fileSystem = fileSystemWrapper ?? new FileSystemWrapper();
            _mockFeeds = new List<MockFeed>();

            if (useDefaultFeed)
            {
                _mockFeeds.Add(new MockFeed
                {
                    Type = MockFeedType.FeedFromLookUpNugetConfig,
                    Packages = new List<MockFeedPackage>
                    {
                        new MockFeedPackage
                        {
                            PackageId = "global.tool.console.demo",
                            Version = "1.0.4"
                        }
                    }
                });
            }

            if (additionalFeeds != null)
            {
                _mockFeeds.AddRange(additionalFeeds);
            }
        }
        
        public IObtainTransaction CreateObtainTransaction(
            string packageId, 
            string packageVersion = null, 
            FilePath? nugetconfig = null,
            string targetframework = null, 
            string source = null)
        {
            return new ObtainTransaction(
                obtainAndReturnExecutablePath: () => ObtainAndReturnExecutablePath(
                    packageId, 
                    packageVersion,
                    nugetconfig,
                    targetframework, 
                    source), 
                commit: () =>
                {
                    _fileSystem.File.Delete(Path.Combine("toolPath", ".stage", "stagedfile"));

                    if (!_fileSystem.Directory.Exists(_fakeExecutableDirectory))
                    {
                        _fileSystem.Directory.CreateDirectory(_fakeExecutableDirectory);
                    }

                    _fileSystem.File.CreateEmptyFile(Path.Combine(_packageIdVersionDirectory, "project.assets.json"));
                    var fakeExecutable = Path.Combine(_fakeExecutableDirectory, FakeEntrypointName);
                    _fileSystem.File.CreateEmptyFile(fakeExecutable);
                },
                prepare: preparingEnlistment =>
                {
                    if (Directory.Exists(Path.Combine("toolPath", packageId)))
                    {
                        preparingEnlistment.ForceRollback();
                        throw new PackageObtainException(
                            $"A tool with the same PackageId {packageId} {Path.GetFullPath(Path.Combine("toolPath", packageId))} existed."); // TODO loc no checkin
                    }

                    preparingEnlistment.Prepared();
                },
                rollback: () =>
                {
                    _fileSystem.File.Delete(Path.Combine("toolPath", ".stage", "stagedfile"));
                }
            );
        }

        private ToolConfigurationAndExecutablePath ObtainAndReturnExecutablePath(
            string packageId,
            string packageVersion = null,
            FilePath? nugetconfig = null,
            string targetframework = null,
            string source = null)
        {
            _beforeRunObtain();

            PickFeedByNugetConfig(nugetconfig);
            PickFeedBySource(source);

            MockFeedPackage package = _mockFeeds
                .SelectMany(f => f.Packages)
                .Where(p => MatchPackageVersion(p, packageId, packageVersion)).OrderByDescending(p => p.Version)
                .FirstOrDefault();

            if (package == null)
            {
                throw new PackageObtainException("simulated cannot find package");
            }

            packageVersion = package.Version;
            targetframework = targetframework ?? "targetframework";

            _packageIdVersionDirectory = Path.Combine("toolPath", packageId, packageVersion);

            _fakeExecutableDirectory = Path.Combine(_packageIdVersionDirectory,
                packageId, packageVersion, "morefolders", "tools",
                targetframework);

            SimulateStageFile();

            var fakeExecutable = Path.Combine(_fakeExecutableDirectory, FakeEntrypointName);

            return new ToolConfigurationAndExecutablePath(
                toolConfiguration: new ToolConfiguration(FakeCommandName, FakeEntrypointName),
                executable: new FilePath(fakeExecutable));
        }

        private static void SimulateStageFile()
        {
            var stageDirectory = Path.Combine("toolPath", ".stage");
            if (!_fileSystem.Directory.Exists(stageDirectory))
            {
                _fileSystem.Directory.CreateDirectory(stageDirectory);
            }

            _fileSystem.File.CreateEmptyFile(Path.Combine(stageDirectory, "stagedfile"));
        }

        private void PickFeedBySource(string source)
        {
            if (source != null)
            {
                var feed = _mockFeeds.SingleOrDefault(
                    f => f.Type == MockFeedType.Source
                         && f.Uri == source);

                if (feed != null)
                {
                    _mockFeeds = new List<MockFeed>
                    {
                        feed
                    };
                }
                else
                {
                    _mockFeeds = new List<MockFeed>();
                }
            }
        }

        private void PickFeedByNugetConfig(FilePath? nugetconfig)
        {
            if (nugetconfig != null)
            {
                if (!_fileSystem.File.Exists(nugetconfig.Value.Value))
                {
                    throw new PackageObtainException(
                        string.Format(CommonLocalizableStrings.NuGetConfigurationFileDoesNotExist,
                            Path.GetFullPath(nugetconfig.Value.Value)));
                }

                var feed = _mockFeeds.SingleOrDefault(
                    f => f.Type == MockFeedType.ExplicitNugetConfig
                         && f.Uri == nugetconfig.Value.Value);

                if (feed != null)
                {
                    _mockFeeds = new List<MockFeed>
                    {
                        feed
                    };
                }
                else
                {
                    _mockFeeds = new List<MockFeed>();
                }
            }
        }

        private static bool MatchPackageVersion(MockFeedPackage p, string packageId, string packageVersion)
        {
            if (packageVersion == null)
            {
                return p.PackageId == packageId;
            }
            return p.PackageId == packageId && p.Version == packageVersion;
        }
    }
}
