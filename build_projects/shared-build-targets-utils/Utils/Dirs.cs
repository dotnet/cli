using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.DotNet.InternalAbstractions;

namespace Microsoft.DotNet.Cli.Build
{
    public static class Dirs
    {
        public static readonly string RepoRoot = Directory.GetCurrentDirectory();
        public static readonly string Output = Path.Combine(
            RepoRoot,
            "artifacts",
            RuntimeEnvironment.GetRuntimeIdentifier());
        public static readonly string Packages = Path.Combine(Output, "packages");
        public static string NuGetPackages = Environment.GetEnvironmentVariable("NUGET_PACKAGES") ?? GetNuGetPackagesDir();

        private static string GetNuGetPackagesDir()
        {
            return Path.Combine(Dirs.RepoRoot, ".nuget", "packages");
        }
    }
}
