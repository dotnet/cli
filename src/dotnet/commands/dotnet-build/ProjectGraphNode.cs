using Microsoft.DotNet.ProjectModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Tools.Build
{
    public class ProjectGraphNode
    {
        Func<ProjectContext> _projectContextCreator;

        public ProjectGraphNode(ProjectContext projectContext, IEnumerable<ProjectGraphNode> dependencies, bool isRoot = false)
        {
            ProjectContext = projectContext;
            Dependencies = dependencies.ToList();
            IsRoot = isRoot;
        }

        public ProjectContext ProjectContext { get; }

        public IReadOnlyList<ProjectGraphNode> Dependencies { get; }

        public bool IsRoot { get; }
    }
}