// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Microsoft.DotNet.Tools.CrossGen.Operations
{
    public static class FileNameConstants
    {
        public static readonly string DynamicLibPrefix = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "" : "lib";

        public static readonly string DynamicLibSuffix = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".dll" :
                                                         RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? ".dylib" : ".so";

        public static readonly string JITLibName = $"{DynamicLibPrefix}clrjit{DynamicLibSuffix}";
    }
}
