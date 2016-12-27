// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.Sln.Internal;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools.Common;
using System;
using System.IO;
using System.Linq;

namespace Microsoft.DotNet.Tools.Add.ProjectToSolution
{
    internal class AddProjectToSolutionCommand : DotNetSubCommandBase
    {
        public static DotNetSubCommandBase Create()
        {
            var command = new AddProjectToSolutionCommand()
            {
                Name = "project",
                FullName = LocalizableStrings.AppFullName,
                Description = LocalizableStrings.AppDescription,
                HandleRemainingArguments = true,
                ArgumentSeparatorHelpText = LocalizableStrings.AppHelpText,
            };

            command.HelpOption("-h|--help");

            return command;
        }

        public override int Run(string fileOrDirectory)
        {
            SlnFile slnFile = SlnFileFactory.CreateFromFileOrDirectory(fileOrDirectory);

            if (RemainingArguments.Count == 0)
            {
                throw new GracefulException(CommonLocalizableStrings.SpecifyAtLeastOneProjectToAdd);
            }

            PathUtility.EnsureAllPathsExist(RemainingArguments, CommonLocalizableStrings.ProjectDoesNotExist);
            var fullProjectPaths = RemainingArguments.Select((p) => Path.GetFullPath(p)).ToList();

            int preAddProjectCount = slnFile.Projects.Count;
            foreach (var fullProjectPath in fullProjectPaths)
            {
                AddProject(slnFile, fullProjectPath);
            }

            if (slnFile.Projects.Count > preAddProjectCount)
            {
                slnFile.Write();
            }

            return 0;
        }

        private void AddProject(SlnFile slnFile, string fullProjectPath)
        {
            var relativeProjectPath = PathUtility.GetRelativePath(
                PathUtility.EnsureTrailingSlash(slnFile.BaseDirectory),
                fullProjectPath);

            if (slnFile.Projects.Any((p) =>
                    string.Equals(p.FilePath, relativeProjectPath, StringComparison.OrdinalIgnoreCase)))
            {
                Reporter.Output.WriteLine(string.Format(
                    CommonLocalizableStrings.SolutionAlreadyContainsProject,
                    slnFile.FullPath,
                    relativeProjectPath));
            }
            else
            {
                var projectInstance = new ProjectInstance(fullProjectPath);
                var slnProject = new SlnProject
                {
                    Id = GetProjectId(projectInstance),
                    TypeGuid = GetProjectTypeGuid(projectInstance),
                    Name = Path.GetFileNameWithoutExtension(relativeProjectPath),
                    FilePath = relativeProjectPath
                };

                slnFile.Projects.Add(slnProject);
                Reporter.Output.WriteLine(
                    string.Format(CommonLocalizableStrings.ProjectAddedToTheSolution, relativeProjectPath));
            }
        }

        private string GetProjectId(ProjectInstance projectInstance)
        {
            var projectGuidProperty = projectInstance.GetPropertyValue("ProjectGuid");
            var projectGuid = string.IsNullOrEmpty(projectGuidProperty)
                ? Guid.NewGuid()
                : new Guid(projectGuidProperty);
            return projectGuid.ToString("B").ToUpper();
        }

        private string GetProjectTypeGuid(ProjectInstance projectInstance)
        {
            string projectTypeGuid = null;

            var projectTypeGuidProperty = projectInstance.GetPropertyValue("ProjectTypeGuid");
            if (!string.IsNullOrEmpty(projectTypeGuidProperty))
            {
                projectTypeGuid = projectTypeGuidProperty.Split(';').Last();
            }
            else
            {
                projectTypeGuid = projectInstance.GetPropertyValue("DefaultProjectTypeGuid");
            }

            if (string.IsNullOrEmpty(projectTypeGuid))
            {
                throw new GracefulException(CommonLocalizableStrings.UnsupportedProjectType);
            }

            return projectTypeGuid;
        }
    }
}
