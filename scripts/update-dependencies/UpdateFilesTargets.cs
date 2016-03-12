using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.DotNet.Cli.Build.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Versioning;

namespace Microsoft.DotNet.Scripts
{
    public static class UpdateFilesTargets
    {
        private static HttpClient s_client = new HttpClient();

        [Target(nameof(GetDependencies), nameof(ReplaceVersions))]
        public static BuildTargetResult UpdateFiles(BuildTargetContext c) => c.Success();

        [Target]
        public static BuildTargetResult GetDependencies(BuildTargetContext c)
        {
            string coreFxLkgVersion = s_client.GetStringAsync("https://raw.githubusercontent.com/eerhardt/versions/master/corefx/release/1.0.0-rc2/LKG.txt").Result;
            coreFxLkgVersion = coreFxLkgVersion.Trim();

            const string coreFxIdPattern = @"^(?i)((System\..*)|(NETStandard\.Library)|(Microsoft\.CSharp)|(Microsoft\.NETCore.*)|(Microsoft\.TargetingPack\.Private\.(CoreCLR|NETNative))|(Microsoft\.Win32\..*)|(Microsoft\.VisualBasic))$";
            const string coreFxIdExclusionPattern = @"System.CommandLine";

            List<DependencyInfo> dependencyInfos = c.GetDependencyInfo();
            dependencyInfos.Add(new DependencyInfo()
            {
                Name = "CoreFX",
                IdPattern = coreFxIdPattern,
                IdExclusionPattern = coreFxIdExclusionPattern,
                NewReleaseVersion = coreFxLkgVersion
            });

            return c.Success();
        }

        private static List<DependencyInfo> GetDependencyInfo(this BuildTargetContext c)
        {
            const string propertyName = "DependencyInfo";
            List<DependencyInfo> dependencyInfos = c.BuildContext.Get<List<DependencyInfo>>(propertyName);
            if (dependencyInfos == null)
            {
                dependencyInfos = new List<DependencyInfo>();
                c.BuildContext[propertyName] = dependencyInfos;
            }

            return dependencyInfos;
        }

        [Target(nameof(ReplaceProjectJson), nameof(ReplaceCrossGen))]
        public static BuildTargetResult ReplaceVersions(BuildTargetContext c) => c.Success();

        [Target]
        public static BuildTargetResult ReplaceProjectJson(BuildTargetContext c)
        {
            List<DependencyInfo> dependencyInfos = c.GetDependencyInfo();

            IEnumerable<string> projectJsonFiles = Enumerable.Union(
                Directory.GetFiles(Dirs.RepoRoot, "project.json", SearchOption.AllDirectories),
                Directory.GetFiles(Path.Combine(Dirs.RepoRoot, @"src\dotnet\commands\dotnet-new"), "project.json.template", SearchOption.AllDirectories));

            JObject projectRoot;
            foreach (string projectJsonFile in projectJsonFiles)
            {
                try
                {
                    projectRoot = ReadProject(projectJsonFile);
                }
                catch (Exception e)
                {
                    c.Warn($"Non-fatal exception occurred reading '{projectJsonFile}'. Skipping file. Exception: {e}. ");
                    continue;
                }

                bool changedAnyPackage = FindAllDependencyProperties(projectRoot)
                    .Select(dependencyProperty => ReplaceDependencyVersion(dependencyProperty, dependencyInfos))
                    .ToArray()
                    .Any(shouldWrite => shouldWrite);

                if (changedAnyPackage)
                {
                    c.Info($"Writing changes to {projectJsonFile}");
                    WriteProject(projectRoot, projectJsonFile);
                }
            }

            return c.Success();
        }

        private static bool ReplaceDependencyVersion(JProperty dependencyProperty, List<DependencyInfo> dependencyInfos)
        {
            string id = dependencyProperty.Name;
            foreach (DependencyInfo dependencyInfo in dependencyInfos)
            {
                if (Regex.IsMatch(id, dependencyInfo.IdPattern))
                {
                    if (string.IsNullOrEmpty(dependencyInfo.IdExclusionPattern) || !Regex.IsMatch(id, dependencyInfo.IdExclusionPattern))
                    {
                        string version;
                        if (dependencyProperty.Value is JObject)
                        {
                            version = dependencyProperty.Value["version"].Value<string>();
                        }
                        else if (dependencyProperty.Value is JValue)
                        {
                            version = dependencyProperty.Value.ToString();
                        }
                        else
                        {
                            throw new Exception($"Invalid package project.json version {dependencyProperty}");
                        }

                        VersionRange dependencyVersionRange = VersionRange.Parse(version);
                        NuGetVersion dependencyVersion = dependencyVersionRange.MinVersion;

                        string newReleaseVersion = dependencyInfo.NewReleaseVersion;

                        if (!string.IsNullOrEmpty(dependencyVersion.Release) && dependencyVersion.Release != newReleaseVersion)
                        {
                            string newVersion = new NuGetVersion(
                                dependencyVersion.Major,
                                dependencyVersion.Minor,
                                dependencyVersion.Patch,
                                newReleaseVersion,
                                dependencyVersion.Metadata).ToNormalizedString();

                            if (dependencyProperty.Value is JObject)
                            {
                                dependencyProperty.Value["version"] = newVersion;
                            }
                            else
                            {
                                dependencyProperty.Value = newVersion;
                            }

                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static JObject ReadProject(string projectJsonPath)
        {
            using (TextReader projectFileReader = File.OpenText(projectJsonPath))
            {
                var projectJsonReader = new JsonTextReader(projectFileReader);

                var serializer = new JsonSerializer();
                return serializer.Deserialize<JObject>(projectJsonReader);
            }
        }

        private static void WriteProject(JObject projectRoot, string projectJsonPath)
        {
            string projectJson = JsonConvert.SerializeObject(projectRoot, Formatting.Indented);

            File.WriteAllText(projectJsonPath, projectJson + Environment.NewLine);
        }

        private static IEnumerable<JProperty> FindAllDependencyProperties(JObject projectJsonRoot)
        {
            return projectJsonRoot
                .Descendants()
                .OfType<JProperty>()
                .Where(property => property.Name == "dependencies")
                .Select(property => property.Value)
                .SelectMany(o => o.Children<JProperty>());
        }

        private class DependencyInfo
        {
            public string Name { get; set; }
            public string IdPattern { get; set; }
            public string IdExclusionPattern { get; set; }
            public string NewReleaseVersion { get; set; }
        }

        [Target]
        public static BuildTargetResult ReplaceCrossGen(BuildTargetContext c)
        {
            DependencyInfo coreFXInfo = c.GetDependencyInfo().Single(d => d.Name == "CoreFX");

            string compileTargetsPath = Path.Combine(Dirs.RepoRoot, @"scripts\dotnet-cli-build\CompileTargets.cs");
            string compileTargetsContent = File.ReadAllText(compileTargetsPath);

            compileTargetsContent = Regex.Replace(compileTargetsContent, @"rc2-\d+", coreFXInfo.NewReleaseVersion);

            File.WriteAllText(compileTargetsPath, compileTargetsContent, Encoding.UTF8);

            return c.Success();
        }
    }
}
