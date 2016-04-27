// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using NuGet.Frameworks;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.ProjectModel.Graph;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Tools.Build
{
    internal class ProjectGraphCollector
    {
        private readonly bool _collectDependencies;
        private readonly Func<string, NuGetFramework, ProjectContext> _projectContextFactory;
        private readonly Func<string, Project> _projectFactory;

        public ProjectGraphCollector(bool collectDependencies,
            Func<string, NuGetFramework, ProjectContext> projectContextFactory,
            Func<string, Project> projectFactory)
        {
            _collectDependencies = collectDependencies;
            _projectContextFactory = projectContextFactory;
            _projectFactory = projectFactory;
        }

        public IEnumerable<ProjectGraphNode> Collect(IEnumerable<ProjectGraphNode> contexts)
        {
            foreach (var context in contexts)
            {
                if (!_collectDependencies)
                {
                    yield return context;
                    continue;
                }

                var libraries = context.ProjectContext.LibraryManager.GetLibraries();
                var lookup = libraries.ToDictionary(l => l.Identity.Name);
                var root = lookup[context.ProjectContext.ProjectFile.Name];
                yield return TraverseProject((ProjectDescription) root, lookup, context.ProjectContext);
            }
        }

        private ProjectGraphNode TraverseProject(ProjectDescription project, IDictionary<string, LibraryDescription> lookup, ProjectContext context = null)
        {
            var deps = new List<ProjectGraphNode>();
            if (_collectDependencies)
            {
                foreach (var dependency in project.Dependencies)
                {
                    LibraryDescription libraryDescription;
                    if (lookup.TryGetValue(dependency.Name, out libraryDescription))
                    {
                        if (libraryDescription.Identity.Type.Equals(LibraryType.Project))
                        {
                            deps.Add(TraverseProject((ProjectDescription)libraryDescription, lookup));
                        }
                        else
                        {
                            deps.AddRange(TraverseNonProject(libraryDescription, lookup));
                        }
                    }
                }
            }
            var projectTask = context != null ?
                Task.FromResult(context.ProjectFile) :
                Task.Run(() => _projectFactory(project.Path));

            var projectContextTask = context != null ?
                Task.FromResult(context) :
                Task.Run(() => _projectContextFactory(project.Path, project.Framework));

            return new ProjectGraphNode(project.Framework, projectTask, projectContextTask, deps, context != null);
        }

        private IEnumerable<ProjectGraphNode> TraverseNonProject(LibraryDescription root, IDictionary<string, LibraryDescription> lookup)
        {
            Stack<LibraryDescription> libraries = new Stack<LibraryDescription>();
            libraries.Push(root);
            while (libraries.Count > 0)
            {
                var current = libraries.Pop();
                bool foundProject = false;
                foreach (var dependency in current.Dependencies)
                {
                    LibraryDescription libraryDescription;
                    if (lookup.TryGetValue(dependency.Name, out libraryDescription))
                    {
                        if (libraryDescription.Identity.Type.Equals(LibraryType.Project))
                        {
                            foundProject = true;
                            yield return TraverseProject((ProjectDescription) libraryDescription, lookup);
                        }
                        else
                        {
                            libraries.Push(libraryDescription);
                        }
                    }
                }
                // if package didn't have any project dependencies inside remove it from lookup
                // and do not traverse anymore
                if (!foundProject)
                {
                    lookup.Remove(current.Identity.Name);
                }
            }
        }
    }
}