// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using NuGet.Versioning;

namespace Microsoft.DotNet.ProjectModel.Graph
{
    public class LockFilePackageLibrary
    {
        public LockFilePackageLibrary(string name, NuGetVersion version, bool isServiceable, string sha512, IEnumerable<string> files)
        {
            Name = name;
            Version = version;
            IsServiceable = isServiceable;
            Sha512 = sha512;
            Files = files.ToArray();
        }

        public string Name { get; }

        public NuGetVersion Version { get; }

        public bool IsServiceable { get; }

        public string Sha512 { get; }

        public IReadOnlyList<string> Files { get; }
    }
}
