// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.ToolPackage
{
    internal class NuGetPackageLocation
    {
        public NuGetPackageLocation(
            string packageId, 
            string packageVersion = null, 
            FilePath? nugetConfig = null, 
            string source = null)
        {
            PackageId = packageId;
            PackageVersion = packageVersion;
            NugetConfig = nugetConfig;
            Source = source;
        }

        public string PackageId { get; private set; }
        public string PackageVersion { get; set; }
        public FilePath? NugetConfig { get; private set; }
        public string Source { get; private set; }
    }
}
