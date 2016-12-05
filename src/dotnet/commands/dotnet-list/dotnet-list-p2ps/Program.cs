// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Cli.CommandLine;
using Microsoft.DotNet.Cli.Utils;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Tools.List.ProjectToProjectReferences
{
    public class ListProjectToProjectReferencesCommand
    {
        public static int Run(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);

            CommandLineApplication app = new CommandLineApplication(throwOnUnexpectedArg: false)
            {
                Name = "dotnet list p2ps",
                FullName = ".NET Add Project to Project (p2p) reference Command",
                Description = "Command to add project to project (p2p) reference",
                AllowArgumentSeparator = true,
                ArgumentSeparatorHelpText = "Project to project references to add"
            };

            app.HelpOption("-h|--help");

            CommandArgument projectArgument = app.Argument(
                "<PROJECT>",
                "The project file to modify. If a project file is not specified," +
                " it searches the current working directory for an MSBuild file that has" +
                " a file extension that ends in `proj` and uses that file.");

            app.OnExecute(() => {
                if (string.IsNullOrEmpty(projectArgument.Value))
                {
                    throw new GracefulException(CommonLocalizableStrings.RequiredArgumentNotPassed, "<Project>");
                }

                var msbuildProj = MsbuildProject.FromFileOrDirectory(projectArgument.Value);

                var p2ps = msbuildProj.GetProjectToProjectReferences();
                if (p2ps.Count() == 0)
                {
                    throw new GracefulException(LocalizableStrings.NoReferencesFound);
                }

                Reporter.Output.WriteLine($"{CommonLocalizableStrings.ProjectReference}");
                Reporter.Output.WriteLine(new string('-', CommonLocalizableStrings.ProjectReference.Length));
                foreach (var p2p in p2ps)
                {
                    Reporter.Output.WriteLine(p2p.Include);
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
