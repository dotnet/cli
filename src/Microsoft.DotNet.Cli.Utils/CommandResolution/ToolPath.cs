// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Microsoft.DotNet.Cli.Utils
{
    internal static class ToolPath
    {
        public static FileInfo MSBuildDll()
        {
            var envVar = Environment.GetEnvironmentVariable(Constants.MSBUILD_EXE_PATH);
            string msBuildExePath;

            if (string.IsNullOrEmpty(envVar))
            {
                msBuildExePath = Path.Combine(MSBuildToolsPath().FullName, "MSBuild.dll");
            }
            else
            {
                msBuildExePath = envVar;
            }

            return new FileInfo(msBuildExePath);
        }

        public static DirectoryInfo MSBuildExtensionsPath()
        {
            var envVar = Environment.GetEnvironmentVariable(Constants.MSBuildExtensionsPath);
            string msBuildExtensionsPath;

            if (string.IsNullOrEmpty(envVar))
            {
                msBuildExtensionsPath = Path.Combine(
                    AppContext.BaseDirectory,
                    "MSBuildExtensions");
            }
            else
            {
                msBuildExtensionsPath = envVar;
            }

            return new DirectoryInfo(msBuildExtensionsPath);
        }

        public static DirectoryInfo MSBuildToolsPath() =>
            new DirectoryInfo(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "MSBuild"));

        public static DirectoryInfo MSBuildSdksPath()
        {
            var msBuildSDKsPath = Environment.GetEnvironmentVariable(Constants.MSBuildSDKsPath);

            return new DirectoryInfo(
                string.IsNullOrEmpty(msBuildSDKsPath) ?
                    Path.Combine(AppContext.BaseDirectory, "Sdks") :
                    msBuildSDKsPath);
        }

        public static DirectoryInfo RoslynPath() =>
             new DirectoryInfo(
                 Path.Combine(
                    MSBuildToolsPath().FullName,
                    "Roslyn"));

        public static FileInfo TestTargets() =>
             new FileInfo(
                 Path.Combine(
                    MSBuildExtensionsPath().FullName,
                    "Microsoft.TestPlatform.targets"));
    }
}
