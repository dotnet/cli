﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.Extensions.PlatformAbstractions;

using static Microsoft.DotNet.Cli.Build.Framework.BuildHelpers;

namespace Microsoft.DotNet.Cli.Build
{
    public class MsiTargets
    {
        private const string ENGINE = "engine.exe";

        private const string WixVersion = "3.10.2";
        private const string CrtVersion = "14.0.23506.0";

        private static string WixRoot
        {
            get
            {
                return Path.Combine(Dirs.Output, $"WixTools.{WixVersion}");
            }
        }

        private static string CrtRedistributablePath
        {
            get
            {
                return Path.Combine(Dirs.Output, "Crt", $"VC_redist.{Arch}.{CrtVersion}.exe");
            }
        }

        private static string SdkMsi { get; set; }

        private static string SdkBundle { get; set; }

        private static string SharedHostMsi { get; set; }

        private static string SharedFrameworkMsi { get; set; }

        private static string Engine { get; set; }

        private static string MsiVersion { get; set; }

        private static string CliVersion { get; set; }

        private static string Arch { get; } = CurrentArchitecture.Current.ToString();

        private static string Channel { get; set; }

        private static void AcquireWix(BuildTargetContext c)
        {
            if (File.Exists(Path.Combine(WixRoot, "candle.exe")))
            {
                return;
            }

            Directory.CreateDirectory(WixRoot);

            c.Info("Downloading WixTools..");

            DownloadFile($"https://dotnetcli.blob.core.windows.net/build/wix/wix.{WixVersion}.zip", Path.Combine(WixRoot, "WixTools.zip"));

            c.Info("Extracting WixTools..");
            ZipFile.ExtractToDirectory(Path.Combine(WixRoot, "WixTools.zip"), WixRoot);
        }

        private static void AcquireCrt(BuildTargetContext c)
        {
            if (File.Exists(CrtRedistributablePath))
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(CrtRedistributablePath));

            c.Info($"Downloading VC_redist.{Arch}.{CrtVersion}.exe..");

            DownloadFile($"https://dotnetcli.blob.core.windows.net/build/vc/{Arch}/VC_redist.{Arch}.{CrtVersion}.exe", CrtRedistributablePath);
        }

        private static void DownloadFile(string uri, string destinationPath)
        {
            using (var httpClient = new HttpClient())
            {
                var getTask = httpClient.GetStreamAsync(uri);

                using (var outStream = File.OpenWrite(destinationPath))
                {
                    getTask.Result.CopyTo(outStream);
                }
            }
        }

        [Target]
        [BuildPlatforms(BuildPlatform.Windows)]
        public static BuildTargetResult InitMsi(BuildTargetContext c)
        {
            SdkBundle = c.BuildContext.Get<string>("CombinedFrameworkSDKHostInstallerFile");
            SdkMsi = Path.ChangeExtension(SdkBundle, "msi");
            Engine = Path.Combine(Path.GetDirectoryName(SdkBundle), ENGINE);

            SharedHostMsi = Path.ChangeExtension(c.BuildContext.Get<string>("SharedHostInstallerFile"), "msi");
            SharedFrameworkMsi = Path.ChangeExtension(c.BuildContext.Get<string>("SharedFrameworkInstallerFile"), "msi");

            var buildVersion = c.BuildContext.Get<BuildVersion>("BuildVersion");
            MsiVersion = buildVersion.GenerateMsiVersion();
            CliVersion = buildVersion.SimpleVersion;
            Channel = c.BuildContext.Get<string>("Channel");

            AcquireWix(c);
            AcquireCrt(c);
            return c.Success();
        }

        [Target(nameof(MsiTargets.InitMsi),
        nameof(GenerateDotnetSharedHostMsi),
        nameof(GenerateDotnetSharedFrameworkMsi),
        nameof(GenerateCliSdkMsi))]
        [BuildPlatforms(BuildPlatform.Windows)]
        public static BuildTargetResult GenerateMsis(BuildTargetContext c)
        {
            return c.Success();
        }

        [Target]
        [BuildPlatforms(BuildPlatform.Windows)]
        public static BuildTargetResult GenerateCliSdkMsi(BuildTargetContext c)
        {
            var cliSdkRoot = c.BuildContext.Get<string>("CLISDKRoot");
            Cmd("powershell", "-NoProfile", "-NoLogo",
                Path.Combine(Dirs.RepoRoot, "packaging", "windows", "generatemsi.ps1"),
                cliSdkRoot, SdkMsi, WixRoot, MsiVersion, CliVersion, Arch, Channel)
                    .Execute()
                    .EnsureSuccessful();
            return c.Success();
        }

        [Target]
        [BuildPlatforms(BuildPlatform.Windows)]
        public static BuildTargetResult GenerateDotnetSharedHostMsi(BuildTargetContext c)
        {
            var inputDir = c.BuildContext.Get<string>("SharedHostPublishRoot");
            var wixObjRoot = Path.Combine(Dirs.Output, "obj", "wix", "sharedhost");

            if (Directory.Exists(wixObjRoot))
            {
                Utils.DeleteDirectory(wixObjRoot);
            }
            Directory.CreateDirectory(wixObjRoot);

            Cmd("powershell", "-NoProfile", "-NoLogo",
                Path.Combine(Dirs.RepoRoot, "packaging", "host", "windows", "generatemsi.ps1"),
                inputDir, SharedHostMsi, WixRoot, MsiVersion, CliVersion, Arch, wixObjRoot)
                    .Execute()
                    .EnsureSuccessful();
            return c.Success();
        }

        [Target]
        [BuildPlatforms(BuildPlatform.Windows)]
        public static BuildTargetResult GenerateDotnetSharedFrameworkMsi(BuildTargetContext c)
        {
            var inputDir = c.BuildContext.Get<string>("SharedFrameworkPublishRoot");
            var sharedFrameworkNuGetName = Monikers.SharedFrameworkName;
            var sharedFrameworkNuGetVersion = c.BuildContext.Get<string>("SharedFrameworkNugetVersion");
            var upgradeCode = Utils.GenerateGuidFromName($"{sharedFrameworkNuGetName}-{sharedFrameworkNuGetVersion}-{Arch}").ToString().ToUpper();
            var wixObjRoot = Path.Combine(Dirs.Output, "obj", "wix", "sharedframework");

            if (Directory.Exists(wixObjRoot))
            {
                Utils.DeleteDirectory(wixObjRoot);
            }
            Directory.CreateDirectory(wixObjRoot);

            Cmd("powershell", "-NoProfile", "-NoLogo",
                Path.Combine(Dirs.RepoRoot, "packaging", "sharedframework", "windows", "generatemsi.ps1"),
                inputDir, SharedFrameworkMsi, WixRoot, MsiVersion, sharedFrameworkNuGetName, sharedFrameworkNuGetVersion, upgradeCode, Arch, wixObjRoot)
                    .Execute()
                    .EnsureSuccessful();
            return c.Success();
        }


        [Target(nameof(MsiTargets.InitMsi))]
        [BuildPlatforms(BuildPlatform.Windows)]
        public static BuildTargetResult GenerateBundle(BuildTargetContext c)
        {
            Cmd("powershell", "-NoProfile", "-NoLogo",
                Path.Combine(Dirs.RepoRoot, "packaging", "windows", "generatebundle.ps1"),
                SdkMsi, SharedFrameworkMsi, SharedHostMsi, CrtRedistributablePath, SdkBundle, WixRoot, MsiVersion, CliVersion, Arch, Channel)
                    .EnvironmentVariable("Stage2Dir", Dirs.Stage2)
                    .Execute()
                    .EnsureSuccessful();
            return c.Success();
        }

        [Target(nameof(MsiTargets.InitMsi))]
        [BuildPlatforms(BuildPlatform.Windows)]
        public static BuildTargetResult ExtractEngineFromBundle(BuildTargetContext c)
        {
            Cmd($"{WixRoot}\\insignia.exe", "-ib", SdkBundle, "-o", Engine)
                    .Execute()
                    .EnsureSuccessful();
            return c.Success();
        }

        [Target(nameof(MsiTargets.InitMsi))]
        [BuildPlatforms(BuildPlatform.Windows)]
        public static BuildTargetResult ReattachEngineToBundle(BuildTargetContext c)
        {
            Cmd($"{WixRoot}\\insignia.exe", "-ab", Engine, SdkBundle, "-o", SdkBundle)
                    .Execute()
                    .EnsureSuccessful();
            return c.Success();
        }
    }
}
