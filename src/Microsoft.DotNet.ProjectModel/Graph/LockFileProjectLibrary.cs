// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NuGet.Versioning;

namespace Microsoft.DotNet.ProjectModel.Graph
{
    public class LockFileProjectLibrary : IMergeable<LockFileProjectLibrary>
    {
        public string Name { get; set; }

        public NuGetVersion Version { get; set; }

        public string Path { get; set; }

        public string MSBuildProjectPath { get; set; }

        public void MergeWith(LockFileProjectLibrary m)
        {
            Path = Path ?? m.Path;
            MSBuildProjectPath = MSBuildProjectPath ?? m.MSBuildProjectPath;
        }
    }
}