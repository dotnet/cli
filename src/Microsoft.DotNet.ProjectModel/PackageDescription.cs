﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.ProjectModel.Graph;
using Microsoft.DotNet.ProjectModel.Resolution;
using NuGet.ProjectModel;
using NuGet.LibraryModel;

namespace Microsoft.DotNet.ProjectModel
{
    public class PackageDescription : LibraryDescription
    {
        public PackageDescription(
            string path,
            LockFileLibrary package,
            LockFileTargetLibrary lockFileLibrary,
            IEnumerable<ProjectLibraryDependency> dependencies,
            bool compatible,
            bool resolved)
            : base(
                  new LibraryIdentity(package.Name, package.Version, LibraryType.Package),
                  "sha512-" + package.Sha512,
                  path,
                  dependencies: dependencies,
                  framework: null,
                  resolved: resolved,
                  compatible: compatible)
        {
            Library = package;
            Target = lockFileLibrary;
        }

        private LockFileTargetLibrary Target { get; }

        public LockFileLibrary Library { get; }

        public IEnumerable<LockFileItem> RuntimeAssemblies => FilterPlaceholders(Target.RuntimeAssemblies);

        public IEnumerable<LockFileItem> CompileTimeAssemblies => FilterPlaceholders(Target.CompileTimeAssemblies);

        public IEnumerable<LockFileItem> ResourceAssemblies => Target.ResourceAssemblies;

        public IEnumerable<LockFileItem> NativeLibraries => Target.NativeLibraries;

        public IEnumerable<LockFileContentFile> ContentFiles => Target.ContentFiles;

        public IEnumerable<LockFileRuntimeTarget> RuntimeTargets => Target.RuntimeTargets;

        private IEnumerable<LockFileItem> FilterPlaceholders(IList<LockFileItem> items)
        {
            return items.Where(a => !PackageDependencyProvider.IsPlaceholderFile(a.Path));
        }
    }
}
