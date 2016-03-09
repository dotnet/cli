using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
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

            const string coreFxVersionPattern = @"^(?i)((System\..*)|(NETStandard\.Library)|(Microsoft\.CSharp)|(Microsoft\.NETCore.*)|(Microsoft\.TargetingPack\.Private\.(CoreCLR|NETNative))|(Microsoft\.Win32\..*)|(Microsoft\.VisualBasic))$";

            List<DependencyInfo> dependencyInfos = c.GetDependencyInfo();
            dependencyInfos.Add(new DependencyInfo() { IdPattern = coreFxVersionPattern, NewReleaseVersion = coreFxLkgVersion });

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

        [Target]
        public static BuildTargetResult ReplaceVersions(BuildTargetContext c)
        {
            List<DependencyInfo> dependencyInfos = c.GetDependencyInfo();

            string currentDirectory = Directory.GetCurrentDirectory();
            JObject projectRoot;
            foreach (string projectJsonFile in Directory.GetFiles(currentDirectory, "project.json", SearchOption.AllDirectories))
            {
                try
                {
                    projectRoot = ReadProject(projectJsonFile);
                }
                catch (Exception e)
                {
                    c.Warn($"Non-fatal exception occurred reading '{projectJsonFile}'. Exception: {e}. ");
                    continue;
                }

                bool changedAnyPackage = FindAllDependencyProperties(projectRoot)
                    .Select(package => VisitPackage(package, dependencyInfos))
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

        private static bool VisitPackage(JProperty package, List<DependencyInfo> dependencyInfos)
        {
            string id = package.Name;
            foreach (DependencyInfo dependencyInfo in dependencyInfos)
            {
                if (Regex.IsMatch(id, dependencyInfo.IdPattern))
                {
                    string version;
                    if (package.Value is JObject)
                    {
                        version = package.Value["version"].Value<string>();
                    }
                    else if (package.Value is JValue)
                    {
                        version = package.Value.ToString();
                    }
                    else
                    {
                        throw new Exception($"package project.json version {package}");
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

                        if (package.Value is JObject)
                        {
                            package.Value["version"] = newVersion;
                        }
                        else
                        {
                            package.Value = newVersion;
                        }

                        return true;
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
            public string IdPattern { get; set; }
            public string NewReleaseVersion { get; set; }
        }
    }
}
