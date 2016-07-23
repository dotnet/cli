// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.DotNet.Cli.Build;

namespace Microsoft.DotNet.Cli.Build
{
    public class GetNuGetInformation : Task
    {
        [Output]
        public string PackagesDir { get; set; }

        public override bool Execute()
        {
            PackagesDir = Dirs.NuGetPackages;

            return true;
        }
    }
}