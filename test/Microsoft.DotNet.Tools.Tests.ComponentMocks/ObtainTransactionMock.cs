// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Transactions;
using Microsoft.DotNet.ToolPackage;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.Tools.Tests.ComponentMocks
{
    internal class ObtainTransactionMock : IObtainTransaction
    {
        private readonly Action _beforeRunObtain;
        public const string FakeEntrypointName = "SimulatorEntryPoint.dll";
        public const string FakeCommandName = "SimulatorCommand";
        private static IFileSystem _fileSystem;
        private string _fakeExecutableDirectory;
        private readonly string fakeExecutable;
        private List<MockFeed> _mockFeeds;
        private readonly string packageIdVersionDirectory;
        private string packageId;
        private string packageVersion;
        private FilePath? nugetconfig;
        private string targetframework;
        private string source;

        public ObtainTransactionMock(
            string packageId,
            string packageVersion,
            FilePath? nugetconfig,
            string targetframework,
            string source,
            Action beforeRunObtain,
            IFileSystem fileSystem,
            List<MockFeed> mockFeeds)
        {
            this.packageId = packageId;
            this.packageVersion = packageVersion;
            this.nugetconfig = nugetconfig;
            this.targetframework = targetframework;
            this.source = source;

            _beforeRunObtain = beforeRunObtain;
            _fileSystem = fileSystem;
            _mockFeeds = mockFeeds;

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

            packageIdVersionDirectory = Path.Combine("toolPath", packageId, packageVersion);

            _fakeExecutableDirectory = Path.Combine(packageIdVersionDirectory,
                packageId, packageVersion, "morefolders", "tools",
                targetframework);
            fakeExecutable = Path.Combine(_fakeExecutableDirectory, FakeEntrypointName);
        }

        public ToolConfigurationAndExecutablePath ObtainAndReturnExecutablePath(
            string packageId,
            string packageVersion = null,
            FilePath? nugetconfig = null,
            string targetframework = null,
            string source = null)
        {
            _beforeRunObtain();

            PickFeedByNugetConfig(nugetconfig);
            PickFeedBySource(source);

            var stageDirectory = Path.Combine("toolPath", ".stage");
            if (!_fileSystem.Directory.Exists(stageDirectory))
            {
                _fileSystem.Directory.CreateDirectory(stageDirectory);
            }

            _fileSystem.File.CreateEmptyFile(Path.Combine(stageDirectory, "stagedfile"));

            return new ToolConfigurationAndExecutablePath(
                toolConfiguration: new ToolConfiguration(FakeCommandName, FakeEntrypointName),
                executable: new FilePath(fakeExecutable));
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

        public ToolConfigurationAndExecutablePath ObtainAndReturnExecutablePath()
        {
            throw new NotImplementedException();
        }

        public void Commit(Enlistment enlistment)
        {
            _fileSystem.File.Delete(Path.Combine("toolPath", ".stage", "stagedfile"));

            if (!_fileSystem.Directory.Exists(_fakeExecutableDirectory))
            {
                _fileSystem.Directory.CreateDirectory(_fakeExecutableDirectory);
            }

            _fileSystem.File.CreateEmptyFile(Path.Combine(packageIdVersionDirectory, "project.assets.json"));
            _fileSystem.File.CreateEmptyFile(fakeExecutable);
        }

        public void InDoubt(Enlistment enlistment)
        {
            Rollback(enlistment);
        }

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            if (Directory.Exists(Path.Combine("toolPath", ".stage", packageId)))
            {
                preparingEnlistment.ForceRollback();
                throw new PackageObtainException($"A tool with the same PackageId {packageId} existed."); // TODO loc no checkin
            }

            preparingEnlistment.Prepared();
        }

        public void Rollback(Enlistment enlistment)
        {
            _fileSystem.File.Delete(Path.Combine("toolPath", ".stage", "stagedfile"));
        }
    }
}
