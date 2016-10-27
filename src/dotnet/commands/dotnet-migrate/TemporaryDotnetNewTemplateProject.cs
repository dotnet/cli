﻿using Microsoft.Build.Construction;
using Microsoft.DotNet.Cli;
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.DotNet.ProjectJsonMigration;

namespace Microsoft.DotNet.Tools.Migrate
{
    internal class TemporaryDotnetNewTemplateProject
    {
        private const string c_temporaryDotnetNewMSBuildProjectName = "p";

        private readonly string _projectDirectory;

        public ProjectRootElement MSBuildProject { get; }

        public TemporaryDotnetNewTemplateProject(string projectType)
        {
            _projectDirectory = CreateDotnetNewMSBuild(c_temporaryDotnetNewMSBuildProjectName, projectType);
            MSBuildProject = GetMSBuildProject(_projectDirectory);

            Clean();
        }

        public void Clean()
        {
            Directory.Delete(Path.Combine(_projectDirectory, ".."), true);
        }

        private string CreateDotnetNewMSBuild(string projectName, string projectType)
        {
            var tempDir = Path.Combine(
                Path.GetTempPath(),
                this.GetType().Namespace,
                Path.GetRandomFileName(),
                c_temporaryDotnetNewMSBuildProjectName);

            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
            Directory.CreateDirectory(tempDir);

            string[] args;

            switch (projectType?.ToUpperInvariant())
            {
                case "WEB":
                    args = new[] { "-t", "web" };
                    break;
                default:
                    args = new string[] { };
                    break;
            }

            RunCommand("new", args, tempDir);

            return tempDir;
        }

        private ProjectRootElement GetMSBuildProject(string temporaryDotnetNewMSBuildDirectory)
        {
            var templateProjPath = Path.Combine(temporaryDotnetNewMSBuildDirectory,
                c_temporaryDotnetNewMSBuildProjectName + ".csproj");

            return ProjectRootElement.Open(templateProjPath);
        }

        private void RunCommand(string commandToExecute, IEnumerable<string> args, string workingDirectory)
        {
            var command = new DotNetCommandFactory()
                .Create(commandToExecute, args)
                .WorkingDirectory(workingDirectory)
                .CaptureStdOut()
                .CaptureStdErr();

            var commandResult = command.Execute();

            if (commandResult.ExitCode != 0)
            {
                MigrationTrace.Instance.WriteLine(commandResult.StdOut);
                MigrationTrace.Instance.WriteLine(commandResult.StdErr);
                
                throw new Exception($"Failed to run {commandToExecute} in directory: {workingDirectory}");
            }
        }
    }
}
