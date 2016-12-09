// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.DotNet.Cli.Sln.Internal;
using Microsoft.DotNet.Cli.Utils;

namespace Microsoft.DotNet.Tools
{
    internal class MsbuildSolution
    {
        private SlnFile _slnFile;

        public string SolutionFullPath
        {
            get
            {
                return _slnFile.FullPath;
            }
        }

        private MsbuildSolution(string solutionPath)
        {
            _slnFile = new SlnFile();
            try
            {
                _slnFile.Read(solutionPath);
            }
            catch
            {
                throw new GracefulException(CommonLocalizableStrings.InvalidSolution, solutionPath);
            }
        }

        public static MsbuildSolution FromFileOrDirectory(string fileOrDirectory)
        {
            if (File.Exists(fileOrDirectory))
            {
                return FromFile(fileOrDirectory);
            }
            else
            {
                return FromDirectory(fileOrDirectory);
            }
        }

        private static MsbuildSolution FromFile(string solutionPath)
        {
            if (!File.Exists(solutionPath))
            {
                throw new GracefulException(CommonLocalizableStrings.SolutionDoesNotExist, solutionPath);
            }

            return new MsbuildSolution(solutionPath);
        }

        private static MsbuildSolution FromDirectory(string solutionDirectory)
        {
            DirectoryInfo dir;
            try
            {
                dir = new DirectoryInfo(solutionDirectory);
            }
            catch (ArgumentException)
            {
                throw new GracefulException(CommonLocalizableStrings.CouldNotFindSolutionOrDirectory, solutionDirectory);
            }

            if (!dir.Exists)
            {
                throw new GracefulException(CommonLocalizableStrings.CouldNotFindSolutionOrDirectory, solutionDirectory);
            }

            FileInfo[] files = dir.GetFiles("*.sln");
            if (files.Length == 0)
            {
                throw new GracefulException(CommonLocalizableStrings.CouldNotFindSolutionIn, solutionDirectory);
            }

            if (files.Length > 1)
            {
                throw new GracefulException(CommonLocalizableStrings.MoreThanOneSolutionInDirectory, solutionDirectory);
            }

            FileInfo solutionFile = files.First();

            if (!solutionFile.Exists)
            {
                throw new GracefulException(CommonLocalizableStrings.CouldNotFindSolutionIn, solutionDirectory);
            }

            return new MsbuildSolution(solutionFile.FullName);
        }

        public int AddProjectToSolution(IEnumerable<string> projects)
        {
            int numberOfAddedProjects = 0;

            foreach (var projectPath in projects)
            {
                var projectPathNormalized = MsbuildUtilities.NormalizeSlashes(projectPath);
                if (_slnFile.Projects.Any((p) =>
                     string.Equals(p.FilePath, projectPathNormalized, StringComparison.OrdinalIgnoreCase)))
                {
                    Reporter.Output.WriteLine(string.Format(
                        CommonLocalizableStrings.SolutionAlreadyHasAProject,
                        SolutionFullPath,
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
                        TypeGuid = SlnFile.CSharpProjectTypeGuid,
                        Name = Path.GetFileNameWithoutExtension(projectPath),
                        FilePath = projectPathNormalized
                    };

                    _slnFile.Projects.Add(slnProject);

                    numberOfAddedProjects++;
                    Reporter.Output.WriteLine(
                        string.Format(CommonLocalizableStrings.ProjectAddedToTheSolution, projectPath));
                }
            }

            return numberOfAddedProjects;
        }

        public void Save()
        {
            _slnFile.Write();
        }
    }
}
