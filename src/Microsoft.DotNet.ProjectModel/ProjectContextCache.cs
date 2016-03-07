// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.ProjectModel.Graph;
using NuGet.Frameworks;

namespace Microsoft.DotNet.ProjectModel
{
    public class ProjectContextCache
    {
        private readonly LockFileReaderCache _lockFileReaderCache;

        public ProjectContextCache(LockFileReaderCache lockFileReaderCache)
        {
            _lockFileReaderCache = lockFileReaderCache;
        }

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
                    projectContext = new ProjectContextBuilder(ResolveLockFileFromCache)
                           .WithProjectDirectory(projectPath)
                           .WithTargetFramework(framework)
                           .WithRuntimeIdentifiers(runtimeIdentifiers)
                           .Build();

                    _cache.Add(key, projectContext);
                }
                return projectContext;
            }
        }
        private LockFile ResolveLockFileFromCache(string projectDir)
        {
            var projectLockJsonPath = Path.Combine(projectDir, LockFile.FileName);
            return File.Exists(projectLockJsonPath) ?
                        _lockFileReaderCache.Read(Path.Combine(projectDir, LockFile.FileName)) :
                        null;
        }
    }
}