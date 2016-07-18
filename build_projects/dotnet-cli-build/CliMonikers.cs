﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Cli.Build.Framework;

namespace Microsoft.DotNet.Cli.Build
{
    public static class CliMonikers
    {
        public static string GetSdkDebianPackageName(BuildTargetContext c)
        {
            var nugetVersion = c.BuildContext.Get<BuildVersion>("BuildVersion").NuGetVersion;

            return $"dotnet-dev-{nugetVersion}";
        }
    }
}
