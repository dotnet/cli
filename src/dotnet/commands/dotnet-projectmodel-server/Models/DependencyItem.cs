// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using Microsoft.DotNet.Utilities;

namespace Microsoft.DotNet.ProjectModel.Server.Models
{
    public sealed class DependencyItem : IEquatable<DependencyItem>
    {
        public string Name { get; set; }

        public string Version { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as DependencyItem);
        }

        public bool Equals(DependencyItem other)
        {
            return other != null &&
                   string.Equals(Name, other.Name) &&
                   string.Equals(Version, other.Version);
        }

        public override int GetHashCode()
        {
            return Hash.Combine((Name ?? string.Empty).GetHashCode(), (Version ?? string.Empty).GetHashCode());
        }
    }
}
