// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools;
using Microsoft.DotNet.ToolPackage;
using Microsoft.DotNet.PlatformAbstractions;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.Tools.Install.Tool
{
    internal class ProjectRestorer : IProjectRestorer
    {
        public void Restore(
            FilePath projectPath,
            DirectoryPath assetJsonOutput,
            FilePath? nugetconfig,
            string source = null)
        {
            var argsToPassToRestore = new List<string>();

            argsToPassToRestore.Add(projectPath.Value);
            if (nugetconfig != null)
            {
                argsToPassToRestore.Add("--configfile");
                argsToPassToRestore.Add(nugetconfig.Value.Value);
            }

            if (source != null)
            {
                argsToPassToRestore.Add("--source");
                argsToPassToRestore.Add(source);
            }

            argsToPassToRestore.AddRange(new List<string>
            {
                "--runtime",
                GetRuntimeIdentifierWithMacOsHighSierraFallback(),
                $"/p:BaseIntermediateOutputPath={assetJsonOutput.ToQuotedString()}"
            });

            var command = new DotNetCommandFactory(alwaysRunOutOfProc: true)
                .Create("restore", argsToPassToRestore)
                .CaptureStdOut()
                .CaptureStdErr();

            var result = command.Execute();
            if (result.ExitCode != 0)
            {
                throw new PackageObtainException(
                    string.Format(
                        LocalizableStrings.FailedToRestorePackage,
                        result.StartInfo.WorkingDirectory, result.StartInfo.Arguments, result.StdErr, result.StdOut));
            }
        }

        // walk around for https://github.com/dotnet/corefx/issues/26488
        // fallback osx.10.13 to osx
        private static string GetRuntimeIdentifierWithMacOsHighSierraFallback()
        {
            if (RuntimeEnvironment.GetRuntimeIdentifier() == "osx.10.13-x64")
            {
                return "osx-x64";
            }

            return RuntimeEnvironment.GetRuntimeIdentifier();
        }
    }
}
