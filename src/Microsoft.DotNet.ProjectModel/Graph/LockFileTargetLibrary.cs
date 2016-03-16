// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace Microsoft.DotNet.ProjectModel.Graph
{
    public class LockFileTargetLibrary : IMergeable<LockFileTargetLibrary>
    {
        public string Name { get; set; }

        public string Type { get; set; }

        public NuGetFramework TargetFramework { get; set; }

        public NuGetVersion Version { get; set; }

        public IList<PackageDependency> Dependencies { get; set; } = new List<PackageDependency>();

        public ISet<string> FrameworkAssemblies { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public IList<LockFileItem> RuntimeAssemblies { get; set; } = new List<LockFileItem>();

        public IList<LockFileItem> CompileTimeAssemblies { get; set; } = new List<LockFileItem>();

        public IList<LockFileItem> ResourceAssemblies { get; set; } = new List<LockFileItem>();

        public IList<LockFileItem> NativeLibraries { get; set; } = new List<LockFileItem>();

        public IList<LockFileContentFile> ContentFiles { get; set; } = new List<LockFileContentFile>();

        public IList<LockFileRuntimeTarget> RuntimeTargets { get; set; } = new List<LockFileRuntimeTarget>();

        public void MergeWith(LockFileTargetLibrary m)
        {
            TargetFramework = TargetFramework ?? m.TargetFramework;

            if (Dependencies == null || !Dependencies.Any())
            {
                Dependencies = m.Dependencies;
            }

            if (FrameworkAssemblies == null || !FrameworkAssemblies.Any())
            {
                FrameworkAssemblies = m.FrameworkAssemblies;
            }

            if (RuntimeAssemblies == null || !RuntimeAssemblies.Any())
            {
                RuntimeAssemblies = m.RuntimeAssemblies;
            }

            if (CompileTimeAssemblies == null || !CompileTimeAssemblies.Any())
            {
                CompileTimeAssemblies = m.CompileTimeAssemblies;
            }

            if (ResourceAssemblies == null || !ResourceAssemblies.Any())
            {
                ResourceAssemblies = m.ResourceAssemblies;
            }

            if (NativeLibraries == null || !NativeLibraries.Any())
            {
                NativeLibraries = m.NativeLibraries;
            }

            if (ContentFiles == null || !ContentFiles.Any())
            {
                ContentFiles = m.ContentFiles;
            }

            if (RuntimeTargets == null || !RuntimeTargets.Any())
            {
                RuntimeTargets = m.RuntimeTargets;
            }
        }
    }
}
