// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.ProjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Frameworks;

namespace Microsoft.DotNet.Tools.Build
{
    internal class ProjectGraphNode
    {private readonly Task<ProjectContext> _projectContextCreator;
        private readonly Task<Project> _projectCreator;

        public ProjectGraphNode(
            NuGetFramework targetFramework,
            Task<Project> project,
            Task<ProjectContext> projectContext,
            IEnumerable<ProjectGraphNode> dependencies,
            bool isRoot = false)
        {
            _projectContextCreator = projectContext;
            _projectCreator = project;
            Dependencies = dependencies.ToList();
            TargetFramework = targetFramework;
            IsRoot = isRoot;
        }

        public ProjectContext ProjectContext { get { return _projectContextCreator.GetAwaiter().GetResult(); } }

        public Project Project{ get { return _projectCreator.GetAwaiter().GetResult(); } }

        public IReadOnlyList<ProjectGraphNode> Dependencies { get; }

        public NuGetFramework TargetFramework { get; set; }

        public bool IsRoot { get; }

        public ProjectContextIdentity Identity
        {
            get
            {
                return new ProjectContextIdentity(Project.ProjectFilePath, TargetFramework);
            }
        }
    }
}