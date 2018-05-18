// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.PlatformAbstractions;

namespace Microsoft.DotNet.Tools.Test.Utilities
{
    public static class EnvironmentInfo
    {
        public static bool HasSharedFramework(string framework)
        {
            if (framework == "netcoreapp1.0")
            {
                string rid = RuntimeEnvironment.GetRuntimeIdentifier();
                switch (rid)
                {
                    case "fedora.27-x64":
                    case "fedora.28-x64":
                    case "opensuse.42.3-x64":
                    case "ubuntu.18.04-x64":
                        return false;
                }
            }

            return true;
        }
    }
}