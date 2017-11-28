// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Microsoft.DotNet.ToolPackage;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.Tools.Tests.ComponentMocks
{
    internal class ToolPackageObtainerMock : IToolPackageObtainer
    {
        private readonly Action _beforeRunObtain;
        public const string FakeEntrypointName = "SimulatorEntryPoint.dll";
        public const string FakeCommandName = "SimulatorCommand";
        private static IFileSystem _fileSystem;
        private string _fakeExecutableDirectory;

        public ToolPackageObtainerMock(IFileSystem fileSystemWrapper = null, Action beforeRunObtain = null)
        {
            _beforeRunObtain = beforeRunObtain ?? (() => { });
            _fileSystem = fileSystemWrapper ?? new FileSystemWrapper();
        }

        public ToolConfigurationAndExecutableDirectory ObtainAndReturnExecutablePath(
            string packageId,
            string packageVersion = null,
            FilePath? nugetconfig = null,
            string targetframework = null,
            string source = null)
        {
            _beforeRunObtain();

            packageVersion = packageVersion ?? "1.2.3";
            targetframework = targetframework ?? "targetframework";

            var packageIdVersionDirectory = Path.Combine("toolPath", packageId, packageVersion);

            _fakeExecutableDirectory = Path.Combine(packageIdVersionDirectory,
                packageId, packageVersion, "morefolders", "tools",
                targetframework);
            var fakeExecutable = Path.Combine(_fakeExecutableDirectory, FakeEntrypointName);

            if (!_fileSystem.Directory.Exists(_fakeExecutableDirectory))
            {
                _fileSystem.Directory.CreateDirectory(_fakeExecutableDirectory);
            }

            _fileSystem.File.CreateEmptyFile(Path.Combine(packageIdVersionDirectory, "project.assets.json"));
            _fileSystem.File.CreateEmptyFile(fakeExecutable);

            return new ToolConfigurationAndExecutableDirectory(
                new ToolConfiguration(FakeCommandName, FakeEntrypointName),
                executableDirectory: new DirectoryPath(_fakeExecutableDirectory));
        }

        public string GetFakeEntryPointPath()
        {
            return _fakeExecutableDirectory;
        }
    }
}
