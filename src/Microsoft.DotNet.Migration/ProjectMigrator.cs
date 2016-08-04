// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Construction;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Cli;
using System.Linq;
using System.IO;

namespace Microsoft.DotNet.Migration
{
    public class ProjectMigrator
    {
        public int Migrate(string projectDirectory, string outputDirectory)
        {
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var projectContexts = ProjectContext.CreateContextForEachTarget(projectDirectory);

            if (projectContexts.Count() > 1)
            {
                throw new Exception("MultiTFM projects currently not supported.");
            }

            if (projectContexts.Count() == 0)
            {
                throw new Exception("No projects found");
            }

            var projectContext = projectContexts.First();

            // Get template
            var csproj = GetDefaultMSBuildProject();

            // Apply Transformations
            var ruleset = new DefaultMigrationRuleSet();
            ruleset.Apply(projectContext, csproj, outputDirectory);

            csproj.Save(Path.Combine(outputDirectory, "output.csproj"));

            return 1;
        }

        private ProjectRootElement GetDefaultMSBuildProject()
        {
            // Get template
            var projectName = "someproject";
            var tempDir = Path.Combine(Path.GetTempPath(), this.GetType().Namespace, projectName);

            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
            Directory.CreateDirectory(tempDir);

            RunCommand("new", new string[] {"-t", "msbuild"}, tempDir);

            var csprojPath = Path.Combine(tempDir, projectName + ".csproj");
            var csproj = ProjectRootElement.Open(csprojPath);
            return csproj;
        }

        private bool RunCommand(string commandToExecute, IEnumerable<string> args, string workingDirectory)
        {
            var command = new DotNetCommandFactory()
                .Create(commandToExecute, args)
                .WorkingDirectory(workingDirectory)
                .CaptureStdOut()
                .CaptureStdErr();

            var commandResult = command.Execute();

            if (commandResult.ExitCode != 0)
            {
                Console.WriteLine(commandResult.StdErr);
                Console.WriteLine(
                    $"Failed to create prime the NuGet cache. {commandToExecute} failed with: {commandResult.ExitCode}");
            }

            return commandResult.ExitCode == 0;
        }
    }
}
