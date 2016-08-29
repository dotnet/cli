// Copyright(c) .NET Foundation and contributors.All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.DotNet.Cli.CommandLine;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Cli;

namespace Microsoft.DotNet.Tools.VSTest
{
    public class VSTestCommand
    {
        public static int Run(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);

            CommandLineApplication cmd = new CommandLineApplication(throwOnUnexpectedArg: false)
            {
                Name = "vstest",
                FullName = "vstest.console",
                Description = "Test Driver for the .NET Platform"
            };

            cmd.HelpOption("-h|--help");

            var arguments = cmd.Argument(
                "Arguments",
                "The container to test and the argument with which test will run, /? -for help",
                multipleValues: true);

            cmd.OnExecute(() =>
            {
                var vstestArgs = new List<string>();

                vstestArgs.AddRange(arguments.Values);

                // Add remaining arguments that the parser did not understand
                vstestArgs.AddRange(cmd.RemainingArguments);

                return new VSTestForwardingApp(vstestArgs).Execute();

            });

            return cmd.Execute(args);
        }
    }
}