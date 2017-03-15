// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Cli.CommandLine;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools.MSBuild;
using Microsoft.DotNet.Cli;
using Parser = Microsoft.DotNet.Cli.Parser;

namespace Microsoft.DotNet.Tools.Restore
{
    public class RestoreCommand : MSBuildForwardingApp
    {
        public RestoreCommand(IEnumerable<string> msbuildArgs, string msbuildPath = null)
            : base(msbuildArgs, msbuildPath)
        {
        }

        public static RestoreCommand FromArgs(string[] args, string msbuildPath = null)
        {
            DebugHelper.HandleDebugSwitch(ref args);

            Reporter.Output.WriteLine(string.Join(" ", args));

            var parser = Parser.Instance;

            var result = parser.ParseFrom("dotnet restore", args);

            result.ShowHelpOrErrorIfAppropriate();

            var parsedRestore = result["dotnet"]["restore"];

            var msbuildArgs = new List<string>
            {
                "/NoLogo",
                "/t:Restore",
                "/ConsoleLoggerParameters:Verbosity=Minimal"
            };

            msbuildArgs.AddRange(parsedRestore.OptionValuesToBeForwarded());

            msbuildArgs.AddRange(parsedRestore.Arguments);
            
            return new RestoreCommand(msbuildArgs, msbuildPath);
        }

        public static int Run(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);

            RestoreCommand cmd;
            try
            {
                cmd = FromArgs(args);
            }
            catch (CommandCreationException e)
            {
                return e.ExitCode;
            }
            
            return cmd.Execute();
        }
    }
}