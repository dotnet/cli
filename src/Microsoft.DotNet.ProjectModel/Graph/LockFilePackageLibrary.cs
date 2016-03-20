// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using NuGet.Versioning;

namespace Microsoft.DotNet.ProjectModel.Graph
{
    public class LockFilePackageLibrary : IMergeable<LockFilePackageLibrary>
    {
        public string Name { get; set; }

        public NuGetVersion Version { get; set; }

        public bool IsServiceable { get; set; }

        public string Sha512 { get; set; }

        public IList<string> Files { get; set; } = new List<string>();

        public string MSBuildProject { get; set; }

        public void MergeWith(LockFilePackageLibrary m)
        {
            // a package gets completely replaced
            Sha512 = m.Sha512;
            IsServiceable = m.IsServiceable;
            Files = m.Files;
        }
    }
}
