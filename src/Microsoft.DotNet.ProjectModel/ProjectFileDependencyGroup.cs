// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.ProjectModel.Graph;
using NuGet.Frameworks;

namespace Microsoft.DotNet.ProjectModel
{
    public class ProjectFileDependencyGroup : IMergeable<ProjectFileDependencyGroup>
    {
        public ProjectFileDependencyGroup(NuGetFramework frameworkName, IEnumerable<string> dependencies)
        {
            FrameworkName = frameworkName;
            Dependencies = dependencies;
        }

        public NuGetFramework FrameworkName { get; }

        public IEnumerable<string> Dependencies { get; private set; }

        public void MergeWith(ProjectFileDependencyGroup m)
        {
            Dependencies = Dependencies?.Union(m.Dependencies) ?? m.Dependencies;
        }
    }
}