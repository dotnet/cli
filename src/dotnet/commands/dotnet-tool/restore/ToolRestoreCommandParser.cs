// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Cli.CommandLine;
using LocalizableStrings = Microsoft.DotNet.Tools.Tool.Restore.LocalizableStrings;

namespace Microsoft.DotNet.Cli
{
    internal static class ToolRestoreCommandParser
    {
        public static Command ToolRestore()
        {
            return Create.Command(
                "restore",
                LocalizableStrings.CommandDescription,
             Accept.ZeroOrOneArgument()
                      .With(name: LocalizableStrings.ManifestPath,
                            description: LocalizableStrings.ManifestPathDescription),
                Create.Option(
                    "--configfile",
                    LocalizableStrings.ConfigFileOptionDescription,
                    Accept.ExactlyOneArgument()
                        .With(name: LocalizableStrings.ConfigFileOptionName)),
                Create.Option(
                    "--add-source",
                    LocalizableStrings.AddSourceOptionDescription,
                    Accept.OneOrMoreArguments()
                          .With(name: LocalizableStrings.AddSourceOptionName)),
                ToolCommandRestorePassThroughOptions.DisableParallelOption(),
                ToolCommandRestorePassThroughOptions.IgnoreFailedSourcesOption(),
                ToolCommandRestorePassThroughOptions.NoCacheOption(),
                CommonOptions.HelpOption(),
                CommonOptions.VerbosityOption());
        }
    }
}
