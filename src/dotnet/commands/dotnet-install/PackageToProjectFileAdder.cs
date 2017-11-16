// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.DotNet.ExecutablePackageObtainer;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.Cli
{

    internal class PackageToProjectFileAdder : ICanAddPackageToProjectFile
    {
        public void Add(FilePath projectPath, string packageId)
        {
            if (projectPath == null) throw new ArgumentNullException(nameof(projectPath));
            if (packageId == null) throw new ArgumentNullException(nameof(packageId));

            var argsToPassToRestore = new List<string>
            {
                projectPath.Value,
                "package",
                packageId,
                "--no-restore"
            };

            var command = new DotNetCommandFactory(alwaysRunOutOfProc: true)
                .Create(
                    "add",
                    argsToPassToRestore)
                .CaptureStdOut()
                .CaptureStdErr();

            var result = command.Execute();
            if (result.ExitCode != 0)
            {
                throw new PackageObtainException("Failed to add package. " +
                                                 $"{Environment.NewLine}WorkingDirectory: " +
                                                 result.StartInfo.WorkingDirectory + 
                                                 $"{Environment.NewLine}Arguments: " +
                                                 result.StartInfo.Arguments + 
                                                 $"{Environment.NewLine}Output: " +
                                                 result.StdErr + result.StdOut);
            }
        }
    }
}
