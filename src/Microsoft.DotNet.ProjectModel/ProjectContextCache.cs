// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using NuGet.Frameworks;

namespace Microsoft.DotNet.ProjectModel
{
    public class ProjectContextCache
    {
        public static ProjectContextCache Default { get; } = new ProjectContextCache();

        private readonly Dictionary<Tuple<string, string, string>, ProjectContext> _cache = new Dictionary<Tuple<string, string, string>, ProjectContext>();

        public ProjectContext Create(string projectPath, NuGetFramework framework, IEnumerable<string> runtimeIdentifiers = null)
        {
            runtimeIdentifiers = runtimeIdentifiers ?? Enumerable.Empty<string>();
            var key = Tuple.Create(projectPath, framework.ToString(), string.Join(",", runtimeIdentifiers));

            lock (_cache)
            {
                ProjectContext projectContext;
                if (!_cache.TryGetValue(key, out projectContext))
                {
                    projectContext = ProjectContext.Create(projectPath, framework, runtimeIdentifiers);
                    _cache.Add(key, projectContext);
                }
                return projectContext;
            }
        }
    }
}