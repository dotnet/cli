// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Cli.CommandLine;
using Microsoft.DotNet.Cli.Utils;
using System.Collections.Generic;

namespace Microsoft.DotNet.Tools.Add.ProjectToProjectReference
{
    public class AddProjectToProjectReferenceCommand
    {
        public static int Run(string projectOrDirectory, string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);

            CommandLineApplication app = new CommandLineApplication(throwOnUnexpectedArg: false)
            {
                Name = "dotnet add <PROJECT> p2p",
                FullName = LocalizableStrings.AppFullName,
                Description = LocalizableStrings.AppDescription,
                HandleRemainingArguments = true,
                ArgumentSeparatorHelpText = LocalizableStrings.AppHelpText
            };

            app.ArgumentHandledByParentCommand(
                "<PROJECT>",
                LocalizableStrings.CmdProjectDescription);

            app.HelpOption("-h|--help");

            CommandOption frameworkOption = app.Option(
                $"-f|--framework <{LocalizableStrings.CmdFramework}>",
                LocalizableStrings.CmdFrameworkDescription,
                CommandOptionType.SingleValue);

            app.OnExecute(() => {
                if (string.IsNullOrEmpty(projectOrDirectory))
                {
                    throw new GracefulException(CommonLocalizableStrings.RequiredArgumentNotPassed, "<PROJECT>");
                }

                var msbuildProj = MsbuildProject.FromFileOrDirectory(projectOrDirectory);

                if (app.RemainingArguments.Count == 0)
                {
                    throw new GracefulException(CommonLocalizableStrings.SpecifyAtLeastOneReferenceToAdd);
                }

                List<string> references = app.RemainingArguments;
                MsbuildProject.EnsureAllReferencesExist(references);
                msbuildProj.ConvertPathsToRelative(ref references);

                int numberOfAddedReferences = msbuildProj.AddProjectToProjectReferences(
                    frameworkOption.Value(),
                    references);

                if (numberOfAddedReferences != 0)
                {
                    msbuildProj.Project.Save();
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
