// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Construction;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.Cli;
using System.Linq;
using System.IO;

namespace Microsoft.DotNet.Migration
{
    public class ProjectMigrator
    {
        // TODO: Test ProjectDependencyMigration
        // TODO: Migrate AssemblyInfo
        // TODO: Migrate PackOptions
        // TODO: Migrate Scripts
        // TODO: Test Configuration Migration
        // TODO: Support Mappings in IncludeContext Transformations
        // TODO: Support Removal of default items/properties from template when appropriate
        // TODO: Specify ordering of generated property/item groups (append at end of file in most cases)
        // TODO: Migrate Multi-TFM projects
        // TODO: Migrate RID projects

        public int Migrate(MigrationSettings settings)
        {
            var outputDirectory = settings.OutputDirectory;
            var projectDirectory = settings.ProjectDirectory;

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var projectContexts = ProjectContext.CreateContextForEachTarget(projectDirectory);
            VerifyProject(projectContexts);

            var projectContext = projectContexts.First();

            // Get template
            var csproj = GetDefaultMSBuildProject();

            // Apply Transformations
            var ruleset = new DefaultMigrationRuleSet();
            ruleset.Apply(projectContext, csproj, outputDirectory);

            csproj.Save(Path.Combine(outputDirectory, projectContext.ProjectFile.Name + ".csproj"));

            return csproj.Count;
        }

        private void VerifyProject(IEnumerable<ProjectContext> projectContexts)
        {
            if (projectContexts.Count() > 1)
            {
                throw new Exception("MultiTFM projects currently not supported.");
            }

            if (projectContexts.Count() == 0)
            {
                throw new Exception("No projects found");
            }

            if (projectContexts.First().RuntimeIdentifier != null)
            {
                throw new Exception("Projects using runtimes node currently not supported.");
            }

            if (projectContexts.First().LockFile == null)
            {
                throw new Exception("Restore must be run prior to project migration.");
            }
        }

        private ProjectRootElement GetDefaultMSBuildProject()
        {
            var guid = Guid.NewGuid().ToString();
            var projectName = "p";

            var tempDir = Path.Combine(Path.GetTempPath(), this.GetType().Namespace, guid, projectName);

            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
            Directory.CreateDirectory(tempDir);

            RunCommand("new", new string[] {"-t", "msbuild"}, tempDir);

            var csprojPath = Path.Combine(tempDir, projectName + ".csproj");
            var csproj = ProjectRootElement.Open(csprojPath);

            Directory.Delete(tempDir, true);

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
