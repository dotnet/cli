// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Construction;

namespace Microsoft.DotNet.Migration
{
    public class ProjectMigrator
    {
        public ProjectMigrator()
        {

        }

        public int Migrate(string projectDirectory, string outputDirectory)
        {
            var projectContexts = ProjectContext.CreateContextForEachTarget(projectDirectory);

            if (projectContexts.Count() > 1)
            {
                throw new Exception("MultiTFM projects currently not supported.");
            }

            if (projectContexts.Count() == 0)
            {
                throw new Exception("No projects found");
            }

            // Get template
            var projectName = "someproject";
            var tempDir = Path.Combine(Path.GetTempPath(), ProjectMigrator.GetType().Namespace, projectName);

            Directory.Delete(tempDir, true);
            Directory.Create(tempDir);

            RunCommand("new", new string[] {"-t", "msbuild"}, tempDir);

            var csprojPath = Path.Combine(tempDir, projectName + ".csproj");
            var cproj = ProjectElementRoot.Open(csprojPath);

            Console.WriteLine(csproj.Count);
            return csproj.Count;
        }

        private bool RunCommand(string commandToExecute, IEnumerable<string> args, string workingDirectory)
        {
            var command = _commandFactory
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
