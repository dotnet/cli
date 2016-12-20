// Copyright (c) .NET Foundation and contributors. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System.IO;
using System.Runtime.InteropServices;
using Microsoft.DotNet.Tools.Test.Utilities;

namespace Microsoft.DotNet.Tests
{
    public static class TestCommandExtensions
    {
        public static TestCommand WithUserProfileRoot(this TestCommand subject, string path)
        {
            var userProfileEnvironmentVariableName = GetUserProfileEnvironmentVariableName();
            return subject.WithEnvironmentVariable(userProfileEnvironmentVariableName, path);
        }
        public static TestCommand WithUserProfileRoot(this TestCommand subject, DirectoryInfo userProfileRoot)
        {
            return subject.WithUserProfileRoot(userProfileRoot.FullName);
        }
        
        private static string GetUserProfileEnvironmentVariableName()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "LocalAppData"
                : "HOME";
        }
    }
}
