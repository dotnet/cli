// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Collections.Generic;
using NuGet.Frameworks;
using NuGet.Versioning;

namespace Microsoft.DotNet.Cli.Utils
{
    public class ToolPathCalculator
    {
        private readonly string _packagesDirectory;

        public ToolPathCalculator(string packagesDirectory)
        {
            _packagesDirectory = packagesDirectory;
        }

        public string GetBestLockFilePath(string packageId, VersionRange versionRange, NuGetFramework framework)
        {
            var availableToolVersions = GetAvailableToolVersions(packageId);

            var bestVersion = versionRange.FindBestMatch(availableToolVersions);

            return GetLockFilePath(packageId, bestVersion, framework);
        }

        public string GetLockFilePath(string packageId, NuGetVersion version, NuGetFramework framework)
        {
            return Path.Combine(
                GetBaseToolPath(packageId),
                version.ToNormalizedString(),
                framework.GetShortFolderName(),
                "project.lock.json");
        }

        private string GetBaseToolPath(string packageId)
        {
            return Path.Combine(
                _packagesDirectory,
                ".tools",
                packageId);
        }

        private IEnumerable<NuGetVersion> GetAvailableToolVersions(string packageId)
        {
            var availableVersions = new List<NuGetVersion>();

            var toolBase = GetBaseToolPath(packageId);
            var versionDirectories = Directory.EnumerateDirectories(toolBase);

            foreach (var versionDirectory in versionDirectories)
            {
                var version = Path.GetFileName(versionDirectory);

                NuGetVersion nugetVersion = null;
                NuGetVersion.TryParse(version, out nugetVersion);

                if (nugetVersion != null)
                {
                    availableVersions.Add(nugetVersion);
                }
            }

            return availableVersions;
        }

    }
}