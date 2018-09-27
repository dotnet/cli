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
    internal struct ToolManifestPackage : IEquatable<ToolManifestPackage>
    {
        public PackageId PackageId { get; }
        public NuGetVersion Version { get; }
        public ToolCommandName[] CommandNames { get; }
        public NuGetFramework OptionalNuGetFramework { get; }

        public ToolManifestPackage(
            PackageId packagePackageId,
            NuGetVersion version,
            ToolCommandName[] toolCommandNames,
            NuGetFramework optionalNuGetFramework = null)
        {
            PackageId = packagePackageId;
            Version = version;
            CommandNames = toolCommandNames;
            OptionalNuGetFramework = optionalNuGetFramework;
        }

        public override bool Equals(object obj)
        {
            return obj is ToolManifestPackage tool &&
                   Equals(tool);
        }

        public bool Equals(ToolManifestPackage other)
        {
            return PackageId.Equals(other.PackageId) &&
                   EqualityComparer<NuGetVersion>.Default.Equals(Version, other.Version) &&
                   CommandNames.SequenceEqual(other.CommandNames) &&
                   EqualityComparer<NuGetFramework>.Default.Equals(OptionalNuGetFramework,
                       other.OptionalNuGetFramework);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PackageId, Version, CommandNames, OptionalNuGetFramework);
        }

        public static bool operator ==(ToolManifestPackage tool1,
            ToolManifestPackage tool2)
        {
            return tool1.Equals(tool2);
        }

        public static bool operator !=(ToolManifestPackage tool1,
            ToolManifestPackage tool2)
        {
            return !(tool1 == tool2);
        }
    }
}
