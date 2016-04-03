// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.ProjectModel
{
    public class PackOptions
    {
        public string[] Tags { get; set; }

        public string[] Owners { get; set; }

        public string ReleaseNotes { get; set; }

        public string IconUrl { get; set; }

        public string ProjectUrl { get; set; }

        public string LicenseUrl { get; set; }

        public bool RequireLicenseAcceptance { get; set; }

        public string RepositoryType { get; set; }

        public string RepositoryUrl { get; set; }

        public IEnumerable<PackIncludeEntry> Include { get; set; }

        public override bool Equals(object obj)
        {
            var other = obj as PackOptions;
            return other != null &&
                EnumerableEquals(Tags, other.Tags) &&
                EnumerableEquals(Owners, other.Owners) &&
                ReleaseNotes == other.ReleaseNotes &&
                IconUrl == other.IconUrl &&
                ProjectUrl == other.ProjectUrl &&
                LicenseUrl == other.LicenseUrl &&
                RequireLicenseAcceptance == other.RequireLicenseAcceptance &&
                RepositoryType == other.RepositoryType &&
                RepositoryUrl == other.RepositoryUrl &&
                EnumerableEquals(Include, other.Include);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        private static bool EnumerableEquals<T>(IEnumerable<T> left, IEnumerable<T> right)
            => Enumerable.SequenceEqual(left ?? EmptyArray<T>.Value, right ?? EmptyArray<T>.Value);

    }
}
