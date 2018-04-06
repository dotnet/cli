// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.DotNet.ShellShim;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.Tools.Tests.ComponentMocks
{
    internal class ShellShimRepositoryMock : IShellShimRepository
    {
        private static IFileSystem _fileSystem;
        private ShellShimRepository _shellShimRepositoryWithMockMaker;
        private readonly DirectoryPath _pathToPlaceShim;

        public ShellShimRepositoryMock(DirectoryPath pathToPlaceShim, IFileSystem fileSystem = null)
        {
            _pathToPlaceShim = pathToPlaceShim;
            _fileSystem = fileSystem ?? new FileSystemWrapper();
            _shellShimRepositoryWithMockMaker = new ShellShimRepository(pathToPlaceShim, fileSystem: fileSystem, appHostShellShimMaker: new AppHostShellShimMakerMock(fileSystem));
        }

        public void CreateShim(FilePath targetExecutablePath, string commandName, IReadOnlyList<FilePath> packagedShim = null)
        {
            _shellShimRepositoryWithMockMaker.CreateShim(targetExecutablePath, commandName, packagedShim);
        }

        public void RemoveShim(string commandName)
        {
            _shellShimRepositoryWithMockMaker.RemoveShim(commandName);
        }
    }
}
