// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Build.Evaluation;
using Microsoft.DotNet.Cli.CommandLine;
using Microsoft.DotNet.Cli.Sln.Internal;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools.Common;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.DotNet.Tools.List.ProjectsInSolution
{
    public class ListProjectsInSolutionCommand
    {
        internal static CommandLineApplication CreateApplication(CommandLineApplication parentApp)
        {
            CommandLineApplication app = parentApp.Command("projects", throwOnUnexpectedArg: false);
            app.FullName = LocalizableStrings.AppFullName;
            app.Description = LocalizableStrings.AppDescription;

            app.HelpOption("-h|--help");

            app.OnExecute(() => {
                try
                {
                    if (!parentApp.Arguments.Any())
                    {
                        throw new GracefulException(CommonLocalizableStrings.RequiredArgumentNotPassed, Constants.ProjectOrSolutionArgumentName);
                    }

                    var projectOrDirectory = parentApp.Arguments.First().Value;
                    if (string.IsNullOrEmpty(projectOrDirectory))
                    {
                        projectOrDirectory = PathUtility.EnsureTrailingSlash(Directory.GetCurrentDirectory());
                    }

                    SlnFile slnFile = SlnFileFactory.CreateFromFileOrDirectory(projectOrDirectory);

                    if (slnFile.Projects.Count == 0)
                    {
                        Reporter.Output.WriteLine(CommonLocalizableStrings.NoProjectsFound);
                        return 0;
                    }

                    Reporter.Output.WriteLine($"{CommonLocalizableStrings.ProjectReferenceOneOrMore}");
                    Reporter.Output.WriteLine(new string('-', CommonLocalizableStrings.ProjectReferenceOneOrMore.Length));
                    foreach (var slnProject in slnFile.Projects)
                    {
                        Reporter.Output.WriteLine(slnProject.FilePath);
                    }

                    return 0;
                }
                catch (GracefulException e)
                {
                    Reporter.Error.WriteLine(e.Message.Red());
                    app.ShowHelp();
                    return 1;
                }
            });

            return app;
        }
    }
}
