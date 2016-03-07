// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.DotNet.ProjectModel.Utilities;
using NuGet.Versioning;

namespace Microsoft.DotNet.ProjectModel.Graph
{
    public class LockFile
    {
        public static readonly int CurrentVersion = 2;
        public static readonly string FileName = "project.lock.json";

        public string LockFilePath { get; }

        public int Version { get; }
        public IReadOnlyList<ProjectFileDependencyGroup> ProjectFileDependencyGroups { get; }
        public IReadOnlyList<LockFilePackageLibrary> PackageLibraries { get; }
        public IReadOnlyList<LockFileProjectLibrary> ProjectLibraries { get; }
        public IReadOnlyList<LockFileTarget> Targets { get; }

        public LockFile(string lockFilePath = null,
            int? version = null,
            IEnumerable<ProjectFileDependencyGroup> projectFileDependencyGroups = null,
            IEnumerable<LockFilePackageLibrary> packageLibraries = null,
            IEnumerable<LockFileProjectLibrary> projectLibraries = null,
            IEnumerable<LockFileTarget> targets = null)
        {
            LockFilePath = lockFilePath;
            Version = version ?? CurrentVersion;
            ProjectFileDependencyGroups = projectFileDependencyGroups?.ToArray() ?? new ProjectFileDependencyGroup[] {};
            PackageLibraries = packageLibraries?.ToArray() ?? new LockFilePackageLibrary[] { };
            ProjectLibraries = projectLibraries?.ToArray() ?? new LockFileProjectLibrary[] { };
            Targets = targets?.ToArray() ?? new LockFileTarget[] { };

        }

        public bool IsValidForProject(Project project)
        {
            string message;
            return IsValidForProject(project, out message);
        }

        public bool IsValidForProject(Project project, out string message)
        {
            if (Version != CurrentVersion)
            {
                message = $"The expected lock file version does not match the actual version";
                return false;
            }

            message = $"Dependencies in {Project.FileName} were modified";

            var actualTargetFrameworks = project.GetTargetFrameworks();

            // The lock file should contain dependencies for each framework plus dependencies shared by all frameworks
            if (ProjectFileDependencyGroups.Count != actualTargetFrameworks.Count() + 1)
            {
                return false;
            }

            foreach (var group in ProjectFileDependencyGroups)
            {
                IOrderedEnumerable<string> actualDependencies;
                var expectedDependencies = group.Dependencies.OrderBy(x => x);

                // If the framework name is empty, the associated dependencies are shared by all frameworks
                if (group.FrameworkName == null)
                {
                    actualDependencies = project.Dependencies
                        .Select(d => d.ToLockFileDependencyGroupString())
                        .OrderBy(x => x, StringComparer.OrdinalIgnoreCase);
                }
                else
                {
                    var framework = actualTargetFrameworks
                        .FirstOrDefault(f => Equals(f.FrameworkName, group.FrameworkName));
                    if (framework == null)
                    {
                        return false;
                    }

                    actualDependencies = framework.Dependencies
                        .Select(d => d.ToLockFileDependencyGroupString())
                        .OrderBy(x => x, StringComparer.OrdinalIgnoreCase);
                }

                if (!actualDependencies.SequenceEqual(expectedDependencies))
                {
                    return false;
                }
            }

            message = null;
            return true;
        }
    }
}