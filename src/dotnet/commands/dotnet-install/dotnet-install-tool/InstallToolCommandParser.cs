// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Cli.CommandLine;

namespace Microsoft.DotNet.Cli
{
    internal static class InstallToolCommandParser
    {
        public static Command InstallTool()
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
                    "Target framework to publish for. The target framework has to be specified in the project file.",
                    Accept.ExactlyOneArgument()),
                CommonOptions.HelpOption());
        }
    }
}
