// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NuGet.Frameworks;

namespace Microsoft.DotNet.Tools.CrossGen
{
    public class CrossGenTarget
    {
        public NuGetFramework Framework { get; private set; }
        public string RuntimeIdentifier { get; private set; }
        public string SharedFrameworkDir { get; private set; }

        public bool IsPortable
        {
            get { return SharedFrameworkDir != null; }
        }

        public CrossGenTarget(NuGetFramework framework, string rid, string sharedFrameworkDir)
        {
            Framework = framework;
            RuntimeIdentifier = rid;
            SharedFrameworkDir = sharedFrameworkDir;
        }
    }
}
