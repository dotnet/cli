// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Cli.CommandLine;
using Microsoft.DotNet.Cli.Utils;
using System.Collections.Generic;

namespace Microsoft.DotNet.Tools.Add.ProjectToSolution
{
    public class AddProjectToSolutionCommand
    {
        public static int Run(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);

            CommandLineApplication app = new CommandLineApplication(throwOnUnexpectedArg: false)
            {
                Name = "dotnet add proj",
                FullName = LocalizableStrings.AppFullName,
                Description = LocalizableStrings.AppDescription,
                AllowArgumentSeparator = true,
                ArgumentSeparatorHelpText = LocalizableStrings.AppHelpText
            };

            app.HelpOption("-h|--help");

            CommandArgument solutionArgument = app.Argument(
                $"<{LocalizableStrings.CmdSolution}>",
                LocalizableStrings.CmdSolutionDescription);

            CommandOption forceOption = app.Option(
                "--force",
                LocalizableStrings.CmdForceDescription,
                CommandOptionType.NoValue);

            app.OnExecute(() => {
                if (string.IsNullOrEmpty(solutionArgument.Value))
                {
                    throw new GracefulException(CommonLocalizableStrings.RequiredArgumentNotPassed, $"<{LocalizableStrings.SolutionException}>");
                }

                var solution = MsbuildSolution.FromFileOrDirectory(solutionArgument.Value);

                if (app.RemainingArguments.Count == 0)
                {
                    throw new GracefulException(CommonLocalizableStrings.SpecifyAtLeastOneProjectToAdd);
                }

                List<string> projects = app.RemainingArguments;
                if (!forceOption.HasValue())
                {
                    MsbuildUtilities.EnsureAllPathsExist(projects, CommonLocalizableStrings.ProjectDoesNotExist);
                    MsbuildUtilities.ConvertPathsToRelative(solution.SolutionFullPath, ref projects);
                }

                int numberOfAddedProjects = solution.AddProjectToSolution(projects);
                if (numberOfAddedProjects != 0)
                {
                    solution.Save();
                }

                return 0;
            });

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
        }
    }
}
