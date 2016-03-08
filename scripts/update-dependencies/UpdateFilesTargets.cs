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
            Dictionary<string, string> dependencyVersions = c.BuildContext.Get<Dictionary<string, string>>("DependencyVersions");
            if (dependencyVersions == null)
            {
                dependencyVersions = new Dictionary<string, string>();
                c.BuildContext["DependencyVersions"] = dependencyVersions;
            }

            string coreFxLkgVersion = s_client.GetStringAsync("https://raw.githubusercontent.com/eerhardt/versions/master/corefx/release/1.0.0-rc2/LKG.txt").Result;

            // TODO: the NuGet ID Regex should go in here
            dependencyVersions.Add("CoreFxVersion", coreFxLkgVersion);

            return c.Success();
        }

        [Target]
        public static BuildTargetResult ReplaceVersions(BuildTargetContext c)
        {
            Dictionary<string, string> dependencyVersions = c.BuildContext.Get<Dictionary<string, string>>("DependencyVersions");

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
                    .Select(package => VisitPackage(package, dependencyVersions))
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

        private static bool VisitPackage(JProperty package, Dictionary<string, string> newDependencyVersions)
        {
            const string coreFxVersionPattern = @"^(?i)((System\..*)|(NETStandard\.Library)|(Microsoft\.CSharp)|(Microsoft\.NETCore.*)|(Microsoft\.TargetingPack\.Private\.(CoreCLR|NETNative))|(Microsoft\.Win32\..*)|(Microsoft\.VisualBasic))$";

            string id = package.Name;
            if (Regex.IsMatch(id, coreFxVersionPattern))
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

                string coreFxVersion = newDependencyVersions["CoreFxVersion"];

                if (!string.IsNullOrEmpty(dependencyVersion.Release) && dependencyVersion.Release != coreFxVersion)
                {
                    string newVersion = new NuGetVersion(
                                   dependencyVersion.Major,
                                   dependencyVersion.Minor,
                                   dependencyVersion.Patch,
                                   coreFxVersion,
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
    }
}
