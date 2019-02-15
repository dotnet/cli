// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.CommandLine;
using Microsoft.DotNet.Cli.Utils;
using Parser = Microsoft.DotNet.Cli.Parser;

namespace Microsoft.DotNet.Tools.Publish
{
    public class PublishCommand : RestoringCommand
    {
        private PublishCommand(
            IEnumerable<string> msbuildArgs,
            IEnumerable<string> userDefinedArguments,
            IEnumerable<string> trailingArguments,
            bool noRestore,
            string msbuildPath = null)
            : base(msbuildArgs, userDefinedArguments, trailingArguments, noRestore, msbuildPath)
        {
        }

        public static PublishCommand FromArgs(string[] args, string msbuildPath = null)
        {
            DebugHelper.HandleDebugSwitch(ref args);

            var msbuildArgs = new List<string>();

            var hasQuietVerbosity = args.Any(arg =>
                arg.Equals("-verbosity:q", StringComparison.Ordinal) ||
                arg.Equals("-verbosity:quiet", StringComparison.Ordinal) ||
                arg.Equals("-verbosity:m", StringComparison.Ordinal) ||
                arg.Equals("-verbosity:minimal", StringComparison.Ordinal)); 

            if (hasQuietVerbosity) {
                msbuildArgs.Add("-nologo");
            }

            var parser = Parser.Instance;

            var result = parser.ParseFrom("dotnet publish", args);

            result.ShowHelpOrErrorIfAppropriate();

            msbuildArgs.Add("-target:Publish");

            var appliedPublishOption = result["dotnet"]["publish"];

            msbuildArgs.AddRange(appliedPublishOption.OptionValuesToBeForwarded());

            msbuildArgs.AddRange(appliedPublishOption.Arguments);

            bool noRestore = appliedPublishOption.HasOption("--no-restore")
                          || appliedPublishOption.HasOption("--no-build");

            return new PublishCommand(
                msbuildArgs,
                appliedPublishOption.OptionValuesToBeForwarded(),
                appliedPublishOption.Arguments,
                noRestore,
                msbuildPath);
        }

        public static int Run(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);

            return FromArgs(args).Execute();
        }
    }
}
