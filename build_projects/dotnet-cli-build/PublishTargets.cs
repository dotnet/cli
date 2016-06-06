using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

using static Microsoft.DotNet.Cli.Build.Framework.BuildHelpers;

namespace Microsoft.DotNet.Cli.Build
{
    public static class PublishTargets
    {
        private static AzurePublisher AzurePublisherTool { get; set; }

        private static DebRepoPublisher DebRepoPublisherTool { get; set; }

        private static string Channel { get; set; }

        private static string Pointer { get; set; }

        private static string CliVersion { get; set; }

        private static string CliNuGetVersion { get; set; }

        private static string SharedFrameworkNugetVersion { get; set; }

        [Target]
        public static BuildTargetResult InitPublish(BuildTargetContext c)
        {
            AzurePublisherTool = new AzurePublisher();
            DebRepoPublisherTool = new DebRepoPublisher(Dirs.Packages);

            CliVersion = c.BuildContext.Get<BuildVersion>("BuildVersion").SimpleVersion;
            CliNuGetVersion = c.BuildContext.Get<BuildVersion>("BuildVersion").NuGetVersion;
            SharedFrameworkNugetVersion = CliDependencyVersions.SharedFrameworkVersion;
            Channel = c.BuildContext.Get<string>("Channel");
            Pointer = "Latest";

            return c.Success();
        }

        [Target(nameof(PrepareTargets.Init),
        nameof(PublishTargets.InitPublish),
        nameof(PublishTargets.PublishArtifacts),
        nameof(PublishTargets.FinalizeBuild),
        nameof(PublishTargets.TriggerDockerHubBuilds))]
        [Environment("PUBLISH_TO_AZURE_BLOB", "1", "true")] // This is set by CI systems
        public static BuildTargetResult Publish(BuildTargetContext c)
        {
            return c.Success();
        }

        [Target]
        public static BuildTargetResult PublishBuildFinalizationMoniker(BuildTargetContext c)
        {
            string moniker = Monikers.GetProductMoniker(c, "build", CliVersion);
            string file = Path.Combine(Path.GetTempPath(), moniker);
            System.IO.File.WriteAllText(file, "");
            AzurePublisherTool.UploadFile(file, AzurePublisher.Product.SDK, CliVersion);

            return c.Success();
        }

        [Target(nameof(PublishTargets.PublishBuildFinalizationMoniker))]
        public static BuildTargetResult FinalizeBuild(BuildTargetContext c)
        {
            if (CheckIfAllBuildsHavePublished())
            {
                PublishCliVersionInformation(c);
                PublishCliNupkgs(c);
            }

            return c.Success();
        }
        private static bool CheckIfAllBuildsHavePublished()
        {
            Dictionary<string, bool> monikers = new Dictionary<string, bool>()
             {
                 { "build-Windows_x86", false },
                 { "build-Windows_x64", false },
                 { "build-Ubuntu_x64", false },
                 { "build-Ubuntu_16_04_x64", false },
                 { "build-RHEL_x64", false },
                 { "build-OSX_x64", false },
                 { "build-Debian_x64", false },
                 { "build-CentOS_x64", false },
                 { "build-Fedora_23_x64", false },
                 { "build-openSUSE_13_2_x64", false }
             };

             IEnumerable<string> blobs = AzurePublisherTool.ListBlobs(AzurePublisher.Product.SDK, CliVersion);
             foreach (string blob in blobs)
             {
                 if (blob.StartsWith("build"))
                 {
                     string name = blob.Substring(0, blob.IndexOf("."));
                     if (monikers.ContainsKey(name))
                     {
                         monikers[name] = true;
                     }
                     else
                     {
                         throw new ArgumentOutOfRangeException("Unknown OS moniker found during build finalization");
                     }
                 }
             }

             return monikers.Keys.All(k => monikers[k]);
        }
        
        public static void PublishCliVersionInformation(BuildTargetContext c)
        {
            var VersionUpdater = new VersionRepoUpdater(EnvVars.EnsureVariable("GITHUB_PASSWORD"));
            var versionBadge = c.BuildContext.Get<string>("VersionBadge");
            VersionUpdater.PublishVersionBadge(versionBadge, Channel, CliVersion, Pointer).Wait();
            VersionUpdater.PublishVersionFile("build.version", Utils.GetCliVersionFileContent(c), Channel, CliVersion, Pointer).Wait();
        }

        public static void PublishCliNupkgs(BuildTargetContext c)
        {
            var VersionUpdater = new VersionRepoUpdater(EnvVars.EnsureVariable("GITHUB_PASSWORD"));
            string nupkgFilePath = EnvVars.EnsureVariable("NUPKG_FILE_PATH");
            string versionsRepoPath = EnvVars.EnsureVariable("VERSIONS_REPO_PATH");
            VersionUpdater.UpdatePublishedVersions(nupkgFilePath, versionsRepoPath).Wait();
        }

        [Target(
            nameof(PublishTargets.PublishInstallerFilesToAzure),
            nameof(PublishTargets.PublishArchivesToAzure)
            // nameof(PublishTargets.PublishDebFilesToDebianRepo), https://github.com/dotnet/cli/issues/2973
                )]
        public static BuildTargetResult PublishArtifacts(BuildTargetContext c) => c.Success();

        [Target(
            nameof(PublishTargets.PublishSdkInstallerFileToAzure),
            nameof(PublishTargets.PublishCombinedFrameworkSDKHostInstallerFileToAzure))]
        public static BuildTargetResult PublishInstallerFilesToAzure(BuildTargetContext c) => c.Success();

        [Target(
            nameof(PublishTargets.PublishCombinedHostFrameworkSdkArchiveToAzure),
            nameof(PublishTargets.PublishCombinedFrameworkSDKArchiveToAzure),
            nameof(PublishTargets.PublishSDKSymbolsArchiveToAzure))]
        public static BuildTargetResult PublishArchivesToAzure(BuildTargetContext c) => c.Success();

        [Target]
        [BuildPlatforms(BuildPlatform.Ubuntu, "14.04")]
        public static BuildTargetResult PublishSdkInstallerFileToAzure(BuildTargetContext c)
        {
            var version = CliNuGetVersion;
            var installerFile = c.BuildContext.Get<string>("SdkInstallerFile");
            var packageName = Monikers.GetSdkDebianPackageName(c);
            string url = AzurePublisherTool.UploadFile(installerFile, AzurePublisher.Product.SDK, version);

            DebRepoPublisherTool.PublishDebFileToDebianRepo(
                packageName,
                version,
                url);

            return c.Success();
        }

        [Target]
        [BuildPlatforms(BuildPlatform.Windows, BuildPlatform.OSX)]
        public static BuildTargetResult PublishCombinedFrameworkSDKHostInstallerFileToAzure(BuildTargetContext c)
        {
            var version = CliNuGetVersion;
            var installerFile = c.BuildContext.Get<string>("CombinedFrameworkSDKHostInstallerFile");
            AzurePublisherTool.UploadFile(installerFile, AzurePublisher.Product.HostAndFrameworkAndSdk, version);

            return c.Success();
        }

        [Target]
        [BuildPlatforms(BuildPlatform.Windows)]
        public static BuildTargetResult PublishCombinedFrameworkSDKArchiveToAzure(BuildTargetContext c)
        {
            var version = CliNuGetVersion;
            var archiveFile = c.BuildContext.Get<string>("CombinedFrameworkSDKCompressedFile");
            AzurePublisherTool.UploadFile(archiveFile, AzurePublisher.Product.FrameworkAndSdk, version);

            return c.Success();
        }

        [Target]
        public static BuildTargetResult PublishCombinedHostFrameworkSdkArchiveToAzure(BuildTargetContext c)
        {
            var version = CliNuGetVersion;
            var archiveFile = c.BuildContext.Get<string>("CombinedFrameworkSDKHostCompressedFile");
            AzurePublisherTool.UploadFile(archiveFile, AzurePublisher.Product.HostAndFrameworkAndSdk, version);

            return c.Success();
        }

        [Target]
        public static BuildTargetResult PublishSDKSymbolsArchiveToAzure(BuildTargetContext c)
        {
            var version = CliNuGetVersion;
            var archiveFile = c.BuildContext.Get<string>("SdkSymbolsCompressedFile");
            AzurePublisherTool.UploadFile(archiveFile, AzurePublisher.Product.SDK, version);

            return c.Success();
        }

        [Target]
        [Environment("DOCKER_HUB_REPO")]
        [Environment("DOCKER_HUB_TRIGGER_TOKEN")]
        public static BuildTargetResult TriggerDockerHubBuilds(BuildTargetContext c)
        {
            string dockerHubRepo = Environment.GetEnvironmentVariable("DOCKER_HUB_REPO");
            string dockerHubTriggerToken = Environment.GetEnvironmentVariable("DOCKER_HUB_TRIGGER_TOKEN");

            Uri baseDockerHubUri = new Uri("https://registry.hub.docker.com/u/");
            Uri dockerHubTriggerUri;
            if (!Uri.TryCreate(baseDockerHubUri, $"{dockerHubRepo}/trigger/{dockerHubTriggerToken}/", out dockerHubTriggerUri))
            {
                return c.Failed("Invalid DOCKER_HUB_REPO and/or DOCKER_HUB_TRIGGER_TOKEN");
            }

            c.Info($"Triggering automated DockerHub builds for {dockerHubRepo}");
            using (HttpClient client = new HttpClient())
            {
                StringContent requestContent = new StringContent("{\"build\": true}", Encoding.UTF8, "application/json");
                try
                {
                    HttpResponseMessage response = client.PostAsync(dockerHubTriggerUri, requestContent).Result;
                    if (!response.IsSuccessStatusCode)
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine($"HTTP request to {dockerHubTriggerUri.ToString()} was unsuccessful.");
                        sb.AppendLine($"Response status code: {response.StatusCode}. Reason phrase: {response.ReasonPhrase}.");
                        sb.Append($"Respone content: {response.Content.ReadAsStringAsync().Result}");
                        return c.Failed(sb.ToString());
                    }
                }
                catch (AggregateException e)
                {
                    return c.Failed($"HTTP request to {dockerHubTriggerUri.ToString()} failed. {e.ToString()}");
                }
            }
            return c.Success();
        }
    }
}

