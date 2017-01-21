using Microsoft.Build.Construction;
using Microsoft.DotNet.Cli;
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ProjectJsonMigration;
using Microsoft.Build.Evaluation;

namespace Microsoft.DotNet.Tools.Migrate
{
    internal class TemporaryDotnetNewTemplateProject
    {
        private const string c_temporaryDotnetNewMSBuildProjectName = "p";

        private readonly string _projectDirectory;

        public ProjectRootElement MSBuildProject { get; }

        public string MSBuildProjectPath
        {
            get
            {
                return Path.Combine(_projectDirectory, c_temporaryDotnetNewMSBuildProjectName + ".csproj");
            }
        }

        public TemporaryDotnetNewTemplateProject()
        {
            _projectDirectory = CreateDotnetNewMSBuild(c_temporaryDotnetNewMSBuildProjectName);
            MSBuildProject = GetMSBuildProject();
        }

        public void Clean()
        {
            Directory.Delete(Path.Combine(_projectDirectory, ".."), true);
        }

        private string CreateDotnetNewMSBuild(string projectName)
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

            RunCommand("new3", new string[] { "console" }, tempDir);

            return tempDir;
        }

        private ProjectRootElement GetMSBuildProject()
        {
            return ProjectRootElement.Open(
                MSBuildProjectPath,
                ProjectCollection.GlobalProjectCollection,
                preserveFormatting: true);
        }

        private void RunCommand(string commandToExecute, IEnumerable<string> args, string workingDirectory)
        {
            List<string> argsWithDirectory = new List<string>(args);
            if (string.Equals(commandToExecute, "new3", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(workingDirectory))
            {
                argsWithDirectory.Add("-o");
                argsWithDirectory.Add(workingDirectory);
            }

            var command = new DotNetCommandFactory()
                .Create(commandToExecute, argsWithDirectory)
                .WorkingDirectory(workingDirectory)
                .CaptureStdOut()
                .CaptureStdErr();

            var commandResult = command.Execute();

            if (commandResult.ExitCode != 0)
            {
                MigrationTrace.Instance.WriteLine(commandResult.StdOut);
                MigrationTrace.Instance.WriteLine(commandResult.StdErr);

                string argList = string.Join(", ", argsWithDirectory);
                throw new GracefulException($"Failed to run {commandToExecute} with args: {argList} ... workingDirectory = {workingDirectory}");
            }
        }
    }
}
