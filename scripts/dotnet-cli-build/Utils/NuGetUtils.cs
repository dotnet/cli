using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using static Microsoft.DotNet.Cli.Build.FS;

namespace Microsoft.DotNet.Cli.Build
{
    internal static class NuGetUtils
    {
        public static void CleanPackagesFromCache(IEnumerable<string> packageIds, bool cleanTools = false)
        {
            foreach (string package in packageIds)
            {
                Rmdir(Path.Combine(Dirs.NuGetPackages, package));

                if (cleanTools)
                {
                    Rmdir(Path.Combine(Dirs.NuGetPackages, ".tools", package));
                }
            }
        }

        public static IEnumerable<string> GetPackageIds(params string[] sourceDirectories)
        {
            Regex nugetFileRegex = new Regex("^(.*?)\\.(([0-9]+\\.)?[0-9]+\\.[0-9]+(-([A-z0-9-]+))?)\\.nupkg$");

            IEnumerable<string> nugetFiles = sourceDirectories
                .SelectMany(d => Directory.GetFiles(d, "*.nupkg"));

            // There may be multiple versions of each package in the source folders, but we
            // only need to return distinct ids.
            HashSet<string> packages = new HashSet<string>();

            foreach (string filePath in nugetFiles)
            {
                Match match = nugetFileRegex.Match(Path.GetFileName(filePath));
                if (match.Success)
                {
                    string packageId = match.Groups[1].Value;
                    packages.Add(packageId);
                }
            }

            return packages;
        }
    }
}
