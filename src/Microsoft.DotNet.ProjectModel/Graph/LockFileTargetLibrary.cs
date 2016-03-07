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
    public class LockFileTargetLibrary
    {
        public LockFileTargetLibrary(string name,
            string type,
            NuGetFramework targetFramework,
            NuGetVersion version,
            IEnumerable<PackageDependency> dependencies = null,
            IEnumerable<string> frameworkAssemblies = null,
            IEnumerable<LockFileItem> runtimeAssemblies = null,
            IEnumerable<LockFileItem> compileTimeAssemblies = null,
            IEnumerable<LockFileItem> resourceAssemblies = null,
            IEnumerable<LockFileItem> nativeLibraries = null,
            IEnumerable<LockFileContentFile> contentFiles = null)
        {
            Name = name;
            Type = type;
            TargetFramework = targetFramework;
            Version = version;
            Dependencies = dependencies?.ToArray() ?? new PackageDependency[] {};
            FrameworkAssemblies = frameworkAssemblies?.ToArray() ?? new string[] {};
            RuntimeAssemblies = runtimeAssemblies?.ToArray() ?? new LockFileItem[] { };
            CompileTimeAssemblies = compileTimeAssemblies?.ToArray() ?? new LockFileItem[] { };
            ResourceAssemblies = resourceAssemblies?.ToArray() ?? new LockFileItem[] { };
            NativeLibraries = nativeLibraries?.ToArray() ?? new LockFileItem[] { };
            ContentFiles = contentFiles?.ToArray() ?? new LockFileContentFile[] { };
        }

        public string Name { get; }

        public string Type { get;  }

        public NuGetFramework TargetFramework { get; }

        public NuGetVersion Version { get; }

        public IReadOnlyList<PackageDependency> Dependencies { get; }

        public IReadOnlyList<string> FrameworkAssemblies { get; }

        public IReadOnlyList<LockFileItem> RuntimeAssemblies { get; }

        public IReadOnlyList<LockFileItem> CompileTimeAssemblies { get; }

        public IReadOnlyList<LockFileItem> ResourceAssemblies { get; }

        public IReadOnlyList<LockFileItem> NativeLibraries { get; }

        public IReadOnlyList<LockFileContentFile> ContentFiles { get; }
    }
}
