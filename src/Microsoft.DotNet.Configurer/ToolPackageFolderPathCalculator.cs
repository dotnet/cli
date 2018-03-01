// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Microsoft.DotNet.Configurer
{
    public static class ToolPackageFolderPathCalculator
    {
        private const string NestedToolPackageFolderName = ".store";
        public static string GetToolPackageFolderPath(string ToolsShimPath)
        {
            return Path.Combine(ToolsShimPath, NestedToolPackageFolderName);
        }
    }
}
