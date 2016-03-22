// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Runtime.Versioning;
using Microsoft.DotNet.ProjectModel.Graph;
using NuGet;
using NuGet.Frameworks;
using NuGet.Versioning;
using NuGet.LibraryModel;
using Library = Microsoft.Extensions.DependencyModel.Library;

namespace Microsoft.DotNet.ProjectModel.Resolution
{
    public class ReferenceAssemblyDependencyResolver
    {
        public ReferenceAssemblyDependencyResolver(FrameworkReferenceResolver frameworkReferenceResolver)
        {
            FrameworkResolver = frameworkReferenceResolver;
        }

        private FrameworkReferenceResolver FrameworkResolver { get; set; }

        public LibraryDescription GetDescription(ProjectLibraryDependency dependency, NuGetFramework targetFramework)
        {
            if (!dependency.LibraryRange.TypeConstraintAllows(LibraryDependencyTarget.Reference))
            {
                return null;
            }

            var name = dependency.Name;
            var version = dependency.LibraryRange.VersionRange?.MinVersion;

            string path;
            Version assemblyVersion;

            if (!FrameworkResolver.TryGetAssembly(name, targetFramework, out path, out assemblyVersion))
            {
                return null;
            }

            return new LibraryDescription(
                new LibraryIdentity(dependency.Name, new NuGetVersion(assemblyVersion), LibraryType.Reference),
                string.Empty, // Framework assemblies don't have hashes
                path,
                Enumerable.Empty<ProjectLibraryDependency>(),
                targetFramework,
                resolved: true,
                compatible: true);
        }
    }
}
