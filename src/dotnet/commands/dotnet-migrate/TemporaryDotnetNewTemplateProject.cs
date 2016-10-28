using Microsoft.Build.Construction;
using Microsoft.DotNet.Cli;
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.DotNet.ProjectJsonMigration;

namespace Microsoft.DotNet.Tools.Migrate
{
    internal class TemporaryDotnetNewTemplateProject
    {
        private static Dictionary<string, TemporaryDotnetNewTemplateProject> _temporaryDotnetNewProjectsByType;

        private static TemporaryDotnetNewTemplateProject _defaultTemplate;

        private const string c_temporaryDotnetNewMSBuildProjectName = "p";

        private readonly string _projectDirectory;

        public ProjectRootElement MSBuildProject { get; }

        private TemporaryDotnetNewTemplateProject(string projectType = null)
        {
            _projectDirectory = CreateDotnetNewMSBuild(c_temporaryDotnetNewMSBuildProjectName, projectType);
            MSBuildProject = GetMSBuildProject(_projectDirectory);

            Clean();
        }

        public static ProjectRootElement DetectProjectType(string projectDirectory, string projectJsonFile)
        {
            ProjectRootElement template = null;
            string projectType;
            if (new ProjectTypeDetector().TryDetectProjectType(projectDirectory, projectJsonFile, out projectType))
            {
                var templateForProjectType = DemandMSBuildTemplateForProjectType(projectType);
                template = (templateForProjectType ?? DefaultTemplate).MSBuildProject;
            }
            else
            {
                template = DefaultTemplate.MSBuildProject;
            }

            return template;
        }

        public static TemporaryDotnetNewTemplateProject DefaultTemplate
        {
            get { return _defaultTemplate ?? (_defaultTemplate = new TemporaryDotnetNewTemplateProject()); }
        }

        private static TemporaryDotnetNewTemplateProject DemandMSBuildTemplateForProjectType(string type)
        {
            if (type == null)
            {
                return null;
            }

            if (_temporaryDotnetNewProjectsByType == null)
            {
                _temporaryDotnetNewProjectsByType = new Dictionary<string, TemporaryDotnetNewTemplateProject>(StringComparer.OrdinalIgnoreCase);
            }

            TemporaryDotnetNewTemplateProject temporaryProject;
            if (!_temporaryDotnetNewProjectsByType.TryGetValue(type, out temporaryProject))
            {
                temporaryProject = new TemporaryDotnetNewTemplateProject(type);
                _temporaryDotnetNewProjectsByType[type] = temporaryProject;
            }

            return temporaryProject;
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

            if (!string.IsNullOrEmpty(projectType))
            {
                RunCommand("new", new []{ "-t", projectType }, tempDir);
            }
            else
            {
                RunCommand("new", new string[] { }, tempDir);
            }

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
