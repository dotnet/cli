
using System;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.CommandLine;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.ToolPackage
{
    internal class PackageLocation
    {
        public PackageLocation(
            FilePath? nugetConfig = null,
            DirectoryPath? rootConfigDirectory = null,
            string[] additionalFeeds = null,
            bool disableParallel = false)
        {
            NugetConfig = nugetConfig;
            RootConfigDirectory = rootConfigDirectory;
            AdditionalFeeds = additionalFeeds ?? Array.Empty<string>();
            DisableParallel = disableParallel;
        }

        public PackageLocation(AppliedOption appliedOption)
        {
            NugetConfig = FilePath.CreateOrReturnNullWhenValueIsNull(appliedOption.ValueOrDefault<string>("configfile"));
            AdditionalFeeds  = appliedOption.ValueOrDefault<string[]>("add-source");
            DisableParallel = appliedOption.ValueOrDefault<bool>("disable-parallel");
        }

        public FilePath? NugetConfig { get; }
        public DirectoryPath? RootConfigDirectory { get; }
        public string[] AdditionalFeeds { get; }
        public bool DisableParallel { get; }
    }
}
