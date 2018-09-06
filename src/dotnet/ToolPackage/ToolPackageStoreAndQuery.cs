// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Tools;
using Microsoft.Extensions.EnvironmentAbstractions;
using NuGet.Versioning;

namespace Microsoft.DotNet.ToolPackage
{
    internal class ToolPackageStoreAndQuery : IToolPackageStoreQuery, IToolPackageStore
    {
        public const string StagingDirectory = ".stage";

        public ToolPackageStoreAndQuery(DirectoryPath root)
        {
            Root = new DirectoryPath(Path.GetFullPath(root.Value));
        }

        public DirectoryPath Root { get; private set; }

        public DirectoryPath GetRandomStagingDirectory()
        {
            return Root.WithSubDirectories(StagingDirectory, Path.GetRandomFileName());
        }

        public NuGetVersion GetStagedPackageVersion(DirectoryPath stagingDirectory, PackageId packageId)
        {
            if (NuGetVersion.TryParse(
                Path.GetFileName(
                    Directory.EnumerateDirectories(
                        stagingDirectory.WithSubDirectories(packageId.ToString()).Value).FirstOrDefault()),
                out var version))
            {
                return version;
            }

            throw new ToolPackageException(
                string.Format(
                    CommonLocalizableStrings.FailedToFindStagedToolPackage,
                    packageId));
        }

        public DirectoryPath GetRootPackageDirectory(PackageId packageId)
        {
            return Root.WithSubDirectories(packageId.ToString());
        }

        public DirectoryPath GetPackageDirectory(PackageId packageId, NuGetVersion version)
        {
            if (version == null)
            {
                throw new ArgumentNullException(nameof(version));
            }

            return GetRootPackageDirectory(packageId)
                .WithSubDirectories(version.ToNormalizedString().ToLowerInvariant());
        }

        public IEnumerable<IToolPackage> EnumeratePackages()
        {
            if (!Directory.Exists(Root.Value))
            {
                yield break;
            }

            foreach (var subdirectory in Directory.EnumerateDirectories(Root.Value))
            {
                var name = Path.GetFileName(subdirectory);
                var packageId = new PackageId(name);

                // Ignore the staging directory and any directory that isn't the same as the package id
                if (name == StagingDirectory || name != packageId.ToString())
                {
                    continue;
                }

                foreach (var package in EnumeratePackageVersions(packageId))
                {
                    yield return package;
                }
            }
        }

        public IEnumerable<IToolPackage> EnumeratePackageVersions(PackageId packageId)
        {
            var packageRootDirectory = Root.WithSubDirectories(packageId.ToString());
            if (!Directory.Exists(packageRootDirectory.Value))
            {
                yield break;
            }

            foreach (var subdirectory in Directory.EnumerateDirectories(packageRootDirectory.Value))
            {
                yield return new ToolPackageInstance(id: packageId,
                    version: NuGetVersion.Parse(Path.GetFileName(subdirectory)),
                    packageDirectory: new DirectoryPath(subdirectory),
                    assetsJsonParentDirectory: new DirectoryPath(subdirectory));
            }
        }

        public IToolPackage GetPackage(PackageId packageId, NuGetVersion version)
        {
            if (version == null)
            {
                throw new ArgumentNullException(nameof(version));
            }

            var directory = GetPackageDirectory(packageId, version);
            if (!Directory.Exists(directory.Value))
            {
                return null;
            }

            return new ToolPackageInstance(id: packageId,
                version: version,
                packageDirectory: directory,
                assetsJsonParentDirectory: directory);
        }
    }
}
