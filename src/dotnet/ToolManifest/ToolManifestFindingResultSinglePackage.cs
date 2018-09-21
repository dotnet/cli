// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ToolPackage;
using NuGet.Frameworks;
using NuGet.Versioning;

namespace Microsoft.DotNet.ToolManifest
{
    internal struct ToolManifestFindingResultSinglePackage : IEquatable<ToolManifestFindingResultSinglePackage>
    {
        public PackageId PackageId { get; }
        public NuGetVersion Version { get; }
        public ToolCommandName[] CommandName { get; }
        public NuGetFramework OptionalNuGetFramework { get; }

        public ToolManifestFindingResultSinglePackage(
            PackageId packagePackageId,
            NuGetVersion version,
            ToolCommandName[] toolCommandName,
            NuGetFramework optionalNuGetFramework = null)
        {
            PackageId = packagePackageId;
            Version = version;
            CommandName = toolCommandName;
            OptionalNuGetFramework = optionalNuGetFramework;
        }

        public override bool Equals(object obj)
        {
            return obj is ToolManifestFindingResultSinglePackage tool &&
                   Equals(tool);
        }

        public bool Equals(ToolManifestFindingResultSinglePackage other)
        {
            return PackageId.Equals(other.PackageId) &&
                   EqualityComparer<NuGetVersion>.Default.Equals(Version, other.Version) &&
                   CommandName.SequenceEqual(other.CommandName) &&
                   EqualityComparer<NuGetFramework>.Default.Equals(OptionalNuGetFramework,
                       other.OptionalNuGetFramework);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PackageId, Version, CommandName, OptionalNuGetFramework);
        }

        public static bool operator ==(ToolManifestFindingResultSinglePackage tool1,
            ToolManifestFindingResultSinglePackage tool2)
        {
            return tool1.Equals(tool2);
        }

        public static bool operator !=(ToolManifestFindingResultSinglePackage tool1,
            ToolManifestFindingResultSinglePackage tool2)
        {
            return !(tool1 == tool2);
        }
    }
}
