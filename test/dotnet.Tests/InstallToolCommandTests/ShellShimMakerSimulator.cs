﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ShellShim;
using Microsoft.Extensions.EnvironmentAbstractions;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Tests.InstallToolCommandTests
{
    internal class ShellShimMakerSimulator : IShellShimMaker
    {
        private static IFileSystem _fileSystem;
        private readonly string _pathToPlaceShim;

        public ShellShimMakerSimulator(string pathToPlaceShim, IFileSystem fileSystem = null)
        {
            _pathToPlaceShim =
                pathToPlaceShim ?? throw new ArgumentNullException(nameof(pathToPlaceShim));

            _fileSystem = fileSystem ?? new FileSystemWrapper();
        }

        public void CreateShim(string packageExecutablePath, string shellCommandName)
        {
            var packageExecutable = new FilePath(packageExecutablePath);


            var fakeshim = new FakeShim
            {
                Runner = "dotnet",
                executablePath = packageExecutable.Value
            };
            var script = JsonConvert.SerializeObject(fakeshim);

            FilePath scriptPath = new FilePath(Path.Combine(_pathToPlaceShim, shellCommandName));
            _fileSystem.File.WriteAllText(scriptPath.Value, script.ToString());
        }

        public void EnsureCommandNameUniqueness(string shellCommandName)
        {
            if (_fileSystem.File.Exists(Path.Combine(_pathToPlaceShim, shellCommandName)))
            {
                throw new GracefulException(
                    $"Failed to install tool {shellCommandName}. A command with the same name already exists.");
            }
        }

        public class FakeShim
        {
            public string Runner { get; set; }
            public string executablePath { get; set; }
        }
    }
}
