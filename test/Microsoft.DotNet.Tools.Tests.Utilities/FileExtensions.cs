// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.DotNet.Tools.Test.Utilities
{
    public static class FileExtensions
    {
        public static string Executable
        {
            get
            {
#if NET451
            return ".exe";
#else
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : "";
#endif   
            }
        }
    }
}