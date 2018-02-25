// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Transactions;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.ToolPackage;
using Microsoft.DotNet.Tools;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.Tools.Tests.ComponentMocks
{
    internal class ToolPackageInstallerMock : IToolPackageInstaller
    {
        private const string ProjectFileName = "TempProject.csproj";

        private readonly IToolPackageStore _store;
        private readonly IProjectRestorer _projectRestorer;
        private readonly IFileSystem _fileSystem;
        private readonly Action _installCallback;

        public ToolPackageInstallerMock(
            IFileSystem fileSystem,
            IToolPackageStore store,
            IProjectRestorer projectRestorer,
            Action installCallback = null)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _projectRestorer = projectRestorer ?? throw new ArgumentNullException(nameof(projectRestorer));
            _installCallback = installCallback;
        }

        public IToolPackage InstallPackage(NuGetPackageLocation nuGetPackageLocation,
            string targetFramework = null,
            string verbosity = null)
        {
            var packageRootDirectory = _store.Root.WithSubDirectories(nuGetPackageLocation.PackageId);
            string rollbackDirectory = null;

            return TransactionalAction.Run<IToolPackage>(
                action: () => {
                    var stageDirectory = _store.Root.WithSubDirectories(".stage", Path.GetRandomFileName());
                    _fileSystem.Directory.CreateDirectory(stageDirectory.Value);
                    rollbackDirectory = stageDirectory.Value;

                    var tempProject = new FilePath(Path.Combine(stageDirectory.Value, ProjectFileName));

                    // Write a fake project with the requested package id, version, and framework
                    _fileSystem.File.WriteAllText(
                        tempProject.Value,
                        $"{nuGetPackageLocation.PackageId}:{nuGetPackageLocation.PackageVersion}:{targetFramework}");

                    // Perform a restore on the fake project
                    _projectRestorer.Restore(
                        tempProject,
                        stageDirectory,
                        nuGetPackageLocation.NugetConfig,
                        nuGetPackageLocation.Source,
                        verbosity);

                    if (_installCallback != null)
                    {
                        _installCallback();
                    }

                    nuGetPackageLocation.PackageVersion = Path.GetFileName(
                        _fileSystem.Directory.EnumerateFileSystemEntries(
                            stageDirectory.WithSubDirectories(nuGetPackageLocation.PackageId).Value).Single());

                    var packageDirectory = packageRootDirectory.WithSubDirectories(nuGetPackageLocation.PackageVersion);
                    if (_fileSystem.Directory.Exists(packageDirectory.Value))
                    {
                        throw new ToolPackageException(
                            string.Format(
                                CommonLocalizableStrings.ToolPackageConflictPackageId,
                                nuGetPackageLocation.PackageId,
                                nuGetPackageLocation.PackageVersion));
                    }

                    _fileSystem.Directory.CreateDirectory(packageRootDirectory.Value);
                    _fileSystem.Directory.Move(stageDirectory.Value, packageDirectory.Value);
                    rollbackDirectory = packageDirectory.Value;

                    return new ToolPackageMock(
                        _fileSystem,
                        nuGetPackageLocation.PackageId,
                        nuGetPackageLocation.PackageVersion,
                        packageDirectory);
                },
                rollback: () => {
                    if (rollbackDirectory != null && _fileSystem.Directory.Exists(rollbackDirectory))
                    {
                        _fileSystem.Directory.Delete(rollbackDirectory, true);
                    }
                    if (_fileSystem.Directory.Exists(packageRootDirectory.Value) &&
                        !_fileSystem.Directory.EnumerateFileSystemEntries(packageRootDirectory.Value).Any())
                    {
                        _fileSystem.Directory.Delete(packageRootDirectory.Value, false);
                    }
                });
        }

        public IReadOnlyList<CommandSettings> InstallPackageToNuGetCache(
            NuGetPackageLocation nuGetPackageLocation,
            string targetFramework = null,
            string verbosity = null,
            DirectoryPath? nugetCacheLocation = null)
        {
            nugetCacheLocation = nugetCacheLocation ?? new DirectoryPath("anypath");

            var packageDirectory = nugetCacheLocation.Value.WithSubDirectories(nuGetPackageLocation.PackageId);
            _fileSystem.Directory.CreateDirectory(packageDirectory.Value);
            var executable = packageDirectory.WithFile("exe");
            _fileSystem.File.CreateEmptyFile(executable.Value);
            return new List<CommandSettings> {new CommandSettings(ProjectRestorerMock.FakeCommandName, "runnner", executable)};
        }
    }
}
