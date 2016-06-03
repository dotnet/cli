using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.InternalAbstractions;

using static Microsoft.DotNet.Cli.Build.Framework.BuildHelpers;

namespace Microsoft.DotNet.Cli.Build
{
    public class DebTargets
    {
        [Target(nameof(GenerateSdkDeb))]
        [BuildPlatforms(BuildPlatform.Ubuntu, "14.04")]
        public static BuildTargetResult GenerateDebs(BuildTargetContext c)
        {
            return c.Success();
        }

        [Target(nameof(InstallSharedFramework))]
        [BuildPlatforms(BuildPlatform.Ubuntu, "14.04")]
        public static BuildTargetResult GenerateSdkDeb(BuildTargetContext c)
        {
            var channel = c.BuildContext.Get<string>("Channel").ToLower();
            var packageName = Monikers.GetSdkDebianPackageName(c);
            var version = c.BuildContext.Get<BuildVersion>("BuildVersion").NuGetVersion;
            var debFile = c.BuildContext.Get<string>("SdkInstallerFile");
            var manPagesDir = Path.Combine(Dirs.RepoRoot, "Documentation", "manpages");
            var sdkPublishRoot = c.BuildContext.Get<string>("CLISDKRoot");
            var sharedFxDebianPackageName = Monikers.GetDebianSharedFrameworkPackageName(CliDependencyVersions.SharedFrameworkVersion);
            var debianConfigFile = Path.Combine(Dirs.DebPackagingConfig, "dotnet-debian_config.json");
            var postinstallFile = Path.Combine(Dirs.DebPackagingConfig, "postinst");

            var debianConfigVariables = new Dictionary<string, string>()
            {
                { "SHARED_FRAMEWORK_DEBIAN_PACKAGE_NAME", sharedFxDebianPackageName },
                { "SHARED_FRAMEWORK_NUGET_NAME", Monikers.SharedFrameworkName },
                { "SHARED_FRAMEWORK_NUGET_VERSION",  CliDependencyVersions.SharedFrameworkVersion },
                { "SHARED_FRAMEWORK_BRAND_NAME", Monikers.SharedFxBrandName },
                { "SDK_NUGET_VERSION", version },
                { "CLI_SDK_BRAND_NAME", Monikers.CLISdkBrandName }
            };

            var debCreator = new DebPackageCreator(
                DotNetCli.Stage2,
                Dirs.Intermediate);

            debCreator.CreateDeb(
                debianConfigFile,
                packageName,
                version,
                sdkPublishRoot,
                debianConfigVariables,
                debFile,
                manpagesDirectory: manPagesDir,
                versionManpages: true,
                debianFiles: new string[] { postinstallFile });

            return c.Success();
        }

        [Target(nameof(InstallSDK),
                nameof(RunE2ETest),
                nameof(RemovePackages))]
        [BuildPlatforms(BuildPlatform.Ubuntu, "14.04")]
        public static BuildTargetResult TestDebInstaller(BuildTargetContext c)
        {
            return c.Success();
        }
        
        [Target]
        public static BuildTargetResult InstallSharedHost(BuildTargetContext c)
        {
            InstallPackage(c.BuildContext.Get<string>("SharedHostInstallerFile"));
            
            return c.Success();
        }
        
        [Target(nameof(InstallSharedHost))]
        public static BuildTargetResult InstallSharedFramework(BuildTargetContext c)
        {
            InstallPackage(c.BuildContext.Get<string>("SharedFrameworkInstallerFile"));
            
            return c.Success();
        }
        
        [Target(nameof(InstallSharedFramework))]
        public static BuildTargetResult InstallSDK(BuildTargetContext c)
        {
            InstallPackage(c.BuildContext.Get<string>("SdkInstallerFile"));
            
            return c.Success();
        }
        
        [Target]
        [BuildPlatforms(BuildPlatform.Ubuntu, "14.04")]
        public static BuildTargetResult RunE2ETest(BuildTargetContext c)
        {
            Directory.SetCurrentDirectory(Path.Combine(Dirs.RepoRoot, "test", "EndToEnd"));
            
            Cmd("dotnet", "build")
                .Execute()
                .EnsureSuccessful();
            
            var testResultsPath = Path.Combine(Dirs.Output, "obj", "debian", "test", "debian-endtoend-testResults.xml");
            
            Cmd("dotnet", "test", "-xml", testResultsPath)
                .Execute()
                .EnsureSuccessful();
            
            return c.Success();
        }
        
        [Target]
        [BuildPlatforms(BuildPlatform.Ubuntu, "14.04")]
        public static BuildTargetResult RemovePackages(BuildTargetContext c)
        {
            IEnumerable<string> orderedPackageNames = new List<string>()
            {
                Monikers.GetSdkDebianPackageName(c),
                Monikers.GetDebianSharedFrameworkPackageName(CliDependencyVersions.SharedFrameworkVersion),
                Monikers.GetDebianSharedHostPackageName(c)
            };
            
            foreach(var packageName in orderedPackageNames)
            {
                RemovePackage(packageName);
            }
            
            return c.Success();
        }
        
        private static void InstallPackage(string packagePath)
        {
            Cmd("sudo", "dpkg", "-i", packagePath)
                .Execute()
                .EnsureSuccessful();
        }
        
        private static void RemovePackage(string packageName)
        {
            Cmd("sudo", "dpkg", "-r", packageName)
                .Execute()
                .EnsureSuccessful();
        }
    }
}
