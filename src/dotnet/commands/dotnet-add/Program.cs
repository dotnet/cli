// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.DotNet.Cli.CommandLine;
using Microsoft.DotNet.Cli.Utils;
using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Construction;
using Microsoft.DotNet.ProjectJsonMigration;
using NuGet.Frameworks;
using Microsoft.DotNet.Tools.Add.ProjectToProjectReference;

namespace Microsoft.DotNet.Tools.Add
{
    public class AddCommand
    {
        private static List<Func<CommandLineApplication, CommandLineApplication>>
            BuiltInCommands => new List<Func<CommandLineApplication, CommandLineApplication>>
        {
            AddProjectToProjectReferenceCommand.CreateApplication,
        };

        public static int Run(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);

            CommandLineApplication app = new CommandLineApplication(throwOnUnexpectedArg: true)
            {
                Name = "dotnet add",
                FullName = LocalizableStrings.NetAddCommand,
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
