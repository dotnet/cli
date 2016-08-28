// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.CommandLine;
using Microsoft.DotNet.Cli.Utils;

namespace Microsoft.DotNet.Tools.Restore3
{
    public class Restore3Command
    {
        public static int Run(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);

            CommandLineApplication cmd = new CommandLineApplication(throwOnUnexpectedArg: false)
            {
                Name = "restore3",
                FullName = "restore3",
                Description = "restore for msbuild"
            };

            cmd.HelpOption("-h|--help");

            var argRoot = cmd.Argument(
                    "[root]",
                    "Optional path to a project file or MSBuild arguments.",
                    multipleValues: true);

            var sourceOption = cmd.Option(
                    "-s|--source <source>",
                    "Specifies a NuGet package source to use during the restore.",
                    CommandOptionType.MultipleValue);

            var packagesOption = cmd.Option(
                    "--packages <packagesDirectory>",
                    "Directory to install packages in.",
                    CommandOptionType.SingleValue);

            var disableParallelOption = cmd.Option(
                    "--disable-parallel",
                    "Disables restoring multiple projects in parallel.",
                    CommandOptionType.NoValue);

            var configFileOption = cmd.Option(
                    "--configfile <file>",
                    "The NuGet configuration file to use.",
                    CommandOptionType.SingleValue);

            var noCacheOption = cmd.Option(
                    "--no-cache",
                    "Do not cache packages and http requests.",
                    CommandOptionType.NoValue);

            var ignoreFailedSourcesOption = cmd.Option(
                    "--ignore-failed-sources",
                    "Treat package source failures as warnings.",
                    CommandOptionType.NoValue);

            cmd.OnExecute(() =>
            {
                var msbuildArgs = new List<string>()
                {
                     "/t:Restore"
                };

                if (sourceOption.HasValue())
                {
                    msbuildArgs.Add($"/p:RestoreSources={string.Join(";", sourceOption.Values)}");
                }

                if (packagesOption.HasValue())
                {
                    msbuildArgs.Add($"/p:RestorePackagesPath={packagesOption.Value()}");
                }

                if (disableParallelOption.HasValue())
                {
                    msbuildArgs.Add($"/p:RestoreDisableParallel=true");
                }

                if (configFileOption.HasValue())
                {
                    msbuildArgs.Add($"/p:RestoreConfigFile={configFileOption.Value()}");
                }

                if (noCacheOption.HasValue())
                {
                    msbuildArgs.Add($"/p:RestoreNoCache=true");
                }

                if (ignoreFailedSourcesOption.HasValue())
                {
                    msbuildArgs.Add($"/p:RestoreIgnoreFailedSources=true");
                }

                // Add in arguments
                msbuildArgs.AddRange(argRoot.Values);

                // Add remaining arguments that the parser did not understand
                msbuildArgs.AddRange(cmd.RemainingArguments);

                return new MSBuildForwardingApp(msbuildArgs).Execute();
            });

            return cmd.Execute(args);
        }
    }
}
