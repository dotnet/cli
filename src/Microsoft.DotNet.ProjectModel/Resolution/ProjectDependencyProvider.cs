// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.ProjectModel.Graph;
using NuGet.Frameworks;
using NuGet.ProjectModel;
using NuGet.LibraryModel;

namespace Microsoft.DotNet.ProjectModel.Resolution
{
    public class ProjectDependencyProvider
    {
        private Func<string, Project> _resolveProject;

        public ProjectDependencyProvider(Func<string, Project> projectCacheResolver)
        {
            _resolveProject = projectCacheResolver;
        }

        public ProjectDescription GetDescription(string name,
                                                 string path,
                                                 LockFileTargetLibrary targetLibrary,
                                                 Func<string, Project> projectCacheResolver)
        {
            var project = _resolveProject(Path.GetDirectoryName(path));
            if (project != null)
            {
                return GetDescription(NuGetFramework.Parse(targetLibrary.Framework), project, targetLibrary);
            }
            else
            {
                return new ProjectDescription(name, path);
            }
        }

        public ProjectDescription GetDescription(string name, string path, LockFileTargetLibrary targetLibrary)
        {
            return GetDescription(name, path, targetLibrary, projectCacheResolver: null);
        }

        public ProjectDescription GetDescription(NuGetFramework targetFramework, Project project, LockFileTargetLibrary targetLibrary)
        {
            // This never returns null
            var targetFrameworkInfo = project.GetTargetFramework(targetFramework);
            var dependencies = new List<ProjectLibraryDependency>(targetFrameworkInfo.Dependencies);

            // Add all of the project's dependencies
            dependencies.AddRange(project.Dependencies);

            if (targetFramework != null && targetFramework.IsDesktop())
            {
                dependencies.Add(new ProjectLibraryDependency(new LibraryRange("mscorlib", LibraryDependencyTarget.Reference)));

                dependencies.Add(new ProjectLibraryDependency(new LibraryRange("System", LibraryDependencyTarget.Reference)));

                if (targetFramework.Version >= new Version(3, 5))
                {
                    dependencies.Add(new ProjectLibraryDependency(new LibraryRange("System.Core", LibraryDependencyTarget.Reference)));

                    if (targetFramework.Version >= new Version(4, 0))
                    {
                        if (!dependencies.Any(dep => string.Equals(dep.Name, "Microsoft.CSharp", StringComparison.OrdinalIgnoreCase)))
                        {
                            dependencies.Add(new ProjectLibraryDependency(new LibraryRange("Microsoft.CSharp", LibraryDependencyTarget.Reference)));
                        }
                    }
                }
            }
            
            if (targetLibrary != null)
            {
                // The lock file entry might have a filtered set of dependencies
                var lockFileDependencies = targetLibrary.Dependencies.ToDictionary(d => d.Id);

                // Remove all non-framework dependencies that don't appear in the lock file entry
                dependencies.RemoveAll(m => !lockFileDependencies.ContainsKey(m.Name) && m.LibraryRange.TypeConstraint != LibraryDependencyTarget.Reference);
            }

            // Mark the library as unresolved if there were specified frameworks
            // and none of them resolved
            bool unresolved = targetFrameworkInfo.FrameworkName == null;

            return new ProjectDescription(
                new LibraryRange(project.Name, LibraryDependencyTarget.All),
                project,
                dependencies,
                targetFrameworkInfo,
                !unresolved);
        }
    }
}
