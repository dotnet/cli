// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.Sln.Internal;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools.Common;
using System;
using System.Collections.Generic;
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
            var relativeProjectPaths = RemainingArguments.Select((p) =>
                PathUtility.GetRelativePath(
                    PathUtility.EnsureTrailingSlash(slnFile.BaseDirectory),
                    Path.GetFullPath(p))).ToList();

            int preAddProjectCount = slnFile.Projects.Count;
            foreach (var project in relativeProjectPaths)
            {
                AddProject(slnFile, project);
            }

            if (slnFile.Projects.Count > preAddProjectCount)
            {
                slnFile.Write();
            }

            return 0;
        }

        private void AddProject(SlnFile slnFile, string projectPath)
        {
            var projectPathNormalized = PathUtility.GetPathWithDirectorySeparator(projectPath);

            if (slnFile.Projects.Any((p) =>
                    string.Equals(p.FilePath, projectPathNormalized, StringComparison.OrdinalIgnoreCase)))
            {
                Reporter.Output.WriteLine(string.Format(
                    CommonLocalizableStrings.SolutionAlreadyContainsProject,
                    slnFile.FullPath,
                    projectPath));
            }
            else
            {
                string projectGuidString = null;
                if (File.Exists(projectPath))
                {
                    var projectElement = ProjectRootElement.Open(
                        projectPath,
                        new ProjectCollection(),
                        preserveFormatting: true);

                    var projectGuidProperty = projectElement.Properties.Where((p) =>
                        string.Equals(p.Name, "ProjectGuid", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                    if (projectGuidProperty != null)
                    {
                        projectGuidString = projectGuidProperty.Value;
                    }
                }

                var projectGuid = (projectGuidString == null)
                    ? Guid.NewGuid()
                    : new Guid(projectGuidString);

                var slnProject = new SlnProject
                {
                    Id = projectGuid.ToString("B").ToUpper(),
                    TypeGuid = ProjectTypeGuids.CPSProjectTypeGuid,
                    Name = Path.GetFileNameWithoutExtension(projectPath),
                    FilePath = projectPathNormalized
                };

                slnFile.Projects.Add(slnProject);

                EnsureBuildConfigurationsAreValidAndComplete(slnFile);

                Reporter.Output.WriteLine(
                    string.Format(CommonLocalizableStrings.ProjectAddedToTheSolution, projectPath));
            }
        }

        private void EnsureBuildConfigurationsAreValidAndComplete(SlnFile slnFile)
        {
            HashSet<string> _buildConfigurations = new HashSet<string>()
            {
                "Debug",
                "Release",
            };

            HashSet<string> _buildPlatforms = new HashSet<string>()
            {
                "Any CPU",
                "x64",
                "x86",
            };

            var invalidConfigs = new List<string>();
            foreach (var configPlatformCombo in slnFile.SolutionConfigurationsSection.Keys)
            {
                var configPlatformComponents = configPlatformCombo.Split('|');
                if (configPlatformComponents.Length != 2)
                {
                    invalidConfigs.Add(configPlatformCombo);
                }
                else
                {
                    var config = configPlatformComponents[0].Trim();
                    if (!_buildConfigurations.Contains(config))
                    {
                        _buildConfigurations.Add(config);
                    }

                    var platform = configPlatformComponents[1].Trim();
                    if (!_buildPlatforms.Contains(platform))
                    {
                        _buildPlatforms.Add(platform);
                    }
                }
            }

            foreach (var invalidConfig in invalidConfigs)
            {
                slnFile.SolutionConfigurationsSection.Remove(invalidConfig);
            }

            var validConfigPlatformCombos = new List<string>();
            foreach (var buildConfig in _buildConfigurations)
            {
                foreach (var buildPlatform in _buildPlatforms)
                {
                    validConfigPlatformCombos.Add($"{buildConfig}|{buildPlatform}");
                }
            }

            foreach (var buildConfig in validConfigPlatformCombos)
            {
                if (!slnFile.SolutionConfigurationsSection.ContainsKey(buildConfig))
                {
                    slnFile.SolutionConfigurationsSection[buildConfig] = buildConfig;
                }
            }

            foreach (var slnProject in slnFile.Projects)
            {
                var buildConfigsPropSet = slnFile.ProjectConfigurationsSection.GetOrCreatePropertySet(slnProject.Id);
                foreach (var buildConfig in slnFile.SolutionConfigurationsSection)
                {
                    var activeCfgKey = $"{buildConfig.Key}.ActiveCfg";
                    if (!buildConfigsPropSet.ContainsKey(activeCfgKey))
                    {
                        buildConfigsPropSet[activeCfgKey] = buildConfig.Value;
                    }

                    var build0Key = $"{buildConfig.Key}.Build.0";
                    if (!buildConfigsPropSet.ContainsKey(build0Key))
                    {
                        buildConfigsPropSet[build0Key] = buildConfig.Value;
                    }
                }
            }
        }
    }
}
