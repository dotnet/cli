﻿using Microsoft.Extensions.PlatformAbstractions;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.DotNet.Cli.Build
{
    public static class Dirs
    {
        public static readonly string RepoRoot = Directory.GetCurrentDirectory();
        public static readonly string Output = Path.Combine(
            RepoRoot,
            "artifacts",
            PlatformServices.Default.Runtime.GetRuntimeIdentifier());

        public static readonly string PackagesIntermediate = Path.Combine(Output, "packages/intermediate");
        public static readonly string Packages = Path.Combine(Output, "packages");
        public static readonly string Stage1 = Path.Combine(Output, "stage1");
        public static readonly string Stage1Compilation = Path.Combine(Output, "stage1compilation");
        public static readonly string Stage2 = Path.Combine(Output, "stage2");
        public static readonly string Stage2Compilation = Path.Combine(Output, "stage2compilation");
        public static readonly string Corehost = Path.Combine(Output, "corehost");
        public static readonly string TestOutput = Path.Combine(Output, "tests");
        public static readonly string TestArtifacts = Path.Combine(TestOutput, "artifacts");
        public static readonly string TestPackages = Path.Combine(TestOutput, "packages");

        public static readonly string OSXReferenceAssembliesPath = "/Library/Frameworks/Mono.framework/Versions/Current/lib/mono/xbuild-frameworks";
        public static readonly string UsrLocalReferenceAssembliesPath = "/usr/local/lib/mono/xbuild-frameworks";
        public static readonly string UsrReferenceAssembliesPath = "/usr/lib/mono/xbuild-frameworks";


        public static string NuGetPackages = Environment.GetEnvironmentVariable("NUGET_PACKAGES") ?? GetNuGetPackagesDir();

        private static string GetNuGetPackagesDir()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE"), ".nuget", "packages");
            }
            return Path.Combine(Environment.GetEnvironmentVariable("HOME"), ".nuget", "packages");
        }
    }
}
