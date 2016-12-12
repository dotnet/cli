// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.DotNet.Cli.CommandLine;
using Microsoft.DotNet.Tools.List.ProjectToProjectReferences;
using Microsoft.DotNet.Cli.Utils;

namespace Microsoft.DotNet.Tools.List
{
    public class ListCommand
    {
        private static List<Func<CommandLineApplication, CommandLineApplication>>
            BuiltInCommands => new List<Func<CommandLineApplication, CommandLineApplication>>
        {
            ListProjectToProjectReferencesCommand.CreateApplication,
        };

        public static int Run(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);

            CommandLineApplication app = new CommandLineApplication(throwOnUnexpectedArg: true)
            {
                Name = "dotnet list",
                FullName = LocalizableStrings.NetListCommand,
            };

            app.HelpOption("-h|--help");

            app.Argument(
                Constants.ProjectOrSolutionArgumentName,
                CommonLocalizableStrings.ArgumentsProjectOrSolutionDescription);

            foreach (var subCommandCreator in BuiltInCommands)
            {
                subCommandCreator(app);
            }

            try
            {
                return app.Execute(args);
            }
            catch (GracefulException e)
            {
                Reporter.Error.WriteLine(e.Message.Red());
                app.ShowHelp();
                return 1;
            }
            catch (CommandParsingException e)
            {
                Reporter.Error.WriteLine(e.Message.Red());
                return 1;
            }
        }
    }
}
