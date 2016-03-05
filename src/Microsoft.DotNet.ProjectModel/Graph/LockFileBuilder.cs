// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.DotNet.ProjectModel.Graph
{
    public class LockFileBuilder
    {
        public string LockFilePath { get; set; }
        public int Version { get; set; }
        public IList<ProjectFileDependencyGroup> ProjectFileDependencyGroups { get; set; } = new List<ProjectFileDependencyGroup>();
        public IList<LockFilePackageLibrary> PackageLibraries { get; set; } = new List<LockFilePackageLibrary>();
        public IList<LockFileProjectLibrary> ProjectLibraries { get; set; } = new List<LockFileProjectLibrary>();
        public IList<LockFileTarget> Targets { get; set; } = new List<LockFileTarget>();

        public LockFile Build()
        {
            return new LockFile(LockFilePath, Version, ProjectFileDependencyGroups, PackageLibraries, ProjectLibraries, Targets);
        }

    }
}