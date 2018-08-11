// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Cli.Utils
{
    public class Product
    {
        public static string LongName => LocalizableStrings.DotNetSdkInfo;
        public static readonly string Version = GetProductVersion();

        private static string GetProductVersion()
        {
            DotnetVersionFile versionFile = DotnetFiles.VersionFileObject;
            return versionFile.BuildNumber ?? string.Empty;
        }
    }
}
