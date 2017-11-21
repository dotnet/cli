// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.ShellShimMaker
{
    public class ShellShimMaker
    {
        private readonly string _pathToPlaceShim;

        public ShellShimMaker(string pathToPlaceShim)
        {
            _pathToPlaceShim =
                pathToPlaceShim ?? throw new ArgumentNullException(nameof(pathToPlaceShim));
        }

        public void CreateShim(string packageExecutablePath, string shellCommandName)
        {
            var packageExecutable = new FilePath(packageExecutablePath);

            var script = new StringBuilder();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                script.AppendLine("@echo off");
                script.AppendLine($"dotnet {packageExecutable.ToEscapedString()} %*");
            }
            else
            {
                script.AppendLine("#!/bin/sh");
                script.AppendLine($"dotnet {packageExecutable.ToEscapedString()} \"$@\"");
            }

            var scriptPath = GetScriptPath(shellCommandName);
            try
            {
                File.WriteAllText(scriptPath.Value, script.ToString());
            }
            catch (UnauthorizedAccessException e)
            {
                throw new GracefulException(
                    string.Format("Install failed, try run the install command as an administrator, if you have the access. {0}",
                        e.Message));
            }

            SetUserExecutionPermissionToShimFile(scriptPath);
        }

        public void EnsureCommandNameUniqueness(string shellCommandName)
        {
            if (File.Exists(Path.Combine(_pathToPlaceShim, shellCommandName)))
            {
                throw new GracefulException($"Failed to create tool {shellCommandName}, a command with the same name existed");
            }
        }

        public void Remove(string shellCommandName)
        {
            File.Delete(GetScriptPath(shellCommandName).Value);
        }

        private FilePath GetScriptPath(string shellCommandName)
        {
            var scriptPath = Path.Combine(_pathToPlaceShim, shellCommandName);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                scriptPath += ".cmd";
            }

            return new FilePath(scriptPath);
        }

        private static void SetUserExecutionPermissionToShimFile(FilePath scriptPath)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

            var result = new CommandFactory()
                .Create("chmod", new[] {"u+x", scriptPath.Value})
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute();


            if (result.ExitCode != 0)
            {
                throw new GracefulException(
                    "Failed to change permission" +
                    $"{Environment.NewLine}error: " + result.StdErr +
                    $"{Environment.NewLine}output: " + result.StdOut);
            }
        }
    }
}
