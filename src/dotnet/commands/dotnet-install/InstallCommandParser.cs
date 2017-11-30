// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Cli.CommandLine;

namespace Microsoft.DotNet.Cli
{
    internal static class InstallCommandParser
    {
        public static Command Install()
        {
            return Create.Command(
                "install", "",
                Accept.NoArguments(), CommonOptions.HelpOption(), InstallTool());
        }

        private static Command InstallTool()
        {
            return Create.Command("tool",
                "Install tool",
                Accept.ExactlyOneArgument(o => "packageId")
                    .With(name: "packageId",
                        description: "Package Id in NuGet"),
                Create.Option(
                    "--version",
                    "Version of the package in NuGet",
                    Accept.ExactlyOneArgument()),
                Create.Option(
                    "--configfile",
                    "NuGet configuration file",
                    Accept.ExactlyOneArgument()),
                Create.Option(
                    "-f|--framework",
                    "Target Framework Moniker (TFM) for the tools to install",
                    Accept.ExactlyOneArgument()),
                CommonOptions.HelpOption());
        }
    }
}
