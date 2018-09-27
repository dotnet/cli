// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ToolPackage;
using Microsoft.Extensions.EnvironmentAbstractions;
using Newtonsoft.Json;
using NuGet.Frameworks;
using NuGet.Versioning;

namespace Microsoft.DotNet.ToolManifest
{
    internal class ToolManifestFinder : IToolManifestFinder
    {
        private readonly DirectoryPath _probStart;
        private readonly IFileSystem _fileSystem;
        private const string _manifestFilenameConvention = "localtool.manifest.json";

        // The supported tool manifest file version.
        private const int SupportedVersion = 1;
        public ToolManifestFinder(DirectoryPath probStart, IFileSystem fileSystem = null)
        {
            _probStart = probStart;
            _fileSystem = fileSystem ?? new FileSystemWrapper();
        }

        public IReadOnlyCollection<ToolManifestPackage> Find(FilePath? filePath = null)
        {
            IEnumerable<FilePath> allPossibleManifests =
                filePath != null
                    ? new[] {filePath.Value}
                    : EnumerateDefaultAllPossibleManifests();

            bool findAnyManifest = false;
            var result = new List<ToolManifestPackage>();
            foreach (FilePath possibleManifest in allPossibleManifests)
            {
                if (_fileSystem.File.Exists(possibleManifest.Value))
                {
                    findAnyManifest = true;
                    SerializableLocalToolsManifest deserializedManifest =
                        DeserializableLocalToolsManifest(possibleManifest);

                    foreach (ToolManifestPackage p in GetToolManifestPackageFromOneManifestFile(deserializedManifest, possibleManifest))
                    {
                        if (!result.Any(addedToolManifestPackages =>
                            addedToolManifestPackages.PackageId.Equals(p.PackageId)))
                        {
                            result.Add(p);
                        }
                    }

                    if (deserializedManifest.isRoot)
                    {
                        return result;
                    }
                }
            }

            if (!findAnyManifest)
            {
                throw new ToolManifestCannotFindException(
                    string.Format(LocalizableStrings.CannotFindAnyManifestsFileSearched,
                        string.Join(Environment.NewLine, allPossibleManifests.Select(f => f.Value))));
            }

            return result;
        }

        private SerializableLocalToolsManifest DeserializableLocalToolsManifest(FilePath possibleManifest)
        {
            return JsonConvert.DeserializeObject<SerializableLocalToolsManifest>(
                _fileSystem.File.ReadAllText(possibleManifest.Value), new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore
                });
        }

        private List<ToolManifestPackage> GetToolManifestPackageFromOneManifestFile(
            SerializableLocalToolsManifest deserializedManifest, FilePath path)
        {
            List<ToolManifestPackage> result = new List<ToolManifestPackage>();
            var errors = new List<string>();

            if (deserializedManifest.version == 0)
            {
                errors.Add(
                    LocalizableStrings.ManifestVersion0 +
                    path.Value);
            }

            if (deserializedManifest.version > SupportedVersion)
            {
                errors.Add(
                    string.Format(
                        LocalizableStrings.ManifestVersionHigherThanSupported,
                        deserializedManifest.version, SupportedVersion) +
                    path.Value);
            }

            foreach (KeyValuePair<string, SerializableLocalToolSinglePackage> tools in deserializedManifest.tools)
            {
                var packageLevelErrors = new List<string>();
                var packageIdString = tools.Key;
                var packageId = new PackageId(packageIdString);

                string versionString = tools.Value.version;
                NuGetVersion version = null;
                if (versionString is null)
                {
                    packageLevelErrors.Add(LocalizableStrings.ToolMissingVersion);
                }
                else
                {
                    var versionParseResult = NuGetVersion.TryParse(
                        versionString, out version);
                    if (!versionParseResult)
                    {
                        packageLevelErrors.Add(string.Format(LocalizableStrings.VersionIsInvalid, versionString));
                    }
                }
                NuGetFramework targetFramework = null;
                var targetFrameworkString = tools.Value.targetFramework;
                if (!(targetFrameworkString is null))
                {
                    targetFramework = NuGetFramework.Parse(
                        targetFrameworkString);

                    if (targetFramework.IsUnsupported)
                    {
                        packageLevelErrors.Add(
                            string.Format(LocalizableStrings.TargetFrameworkIsUnsupported,
                                targetFrameworkString));
                    }
                }

                if (tools.Value.commands == null
                    || (tools.Value.commands != null && tools.Value.commands.Length == 0))
                {
                    packageLevelErrors.Add(LocalizableStrings.FieldCommandsIsMissing);
                }

                if (packageLevelErrors.Any())
                {
                    var joined = string.Join(string.Empty,
                        packageLevelErrors.Select(e => Environment.NewLine + "    " + e));
                    errors.Add(string.Format(LocalizableStrings.InPackage, packageId.ToString()) + joined);
                }
                else
                {
                    result.Add(new ToolManifestPackage(
                        packageId,
                        version,
                        ToolCommandName.Convert(tools.Value.commands)));
                }
            }

            if (errors.Any())
            {
                throw new ToolManifestException(LocalizableStrings.InvalidManifestFilePrefix +
                                                string.Join(string.Empty,
                                                    errors.Select(e => Environment.NewLine + "  " + e)));
            }

            return result;
        }

        private IEnumerable<FilePath> EnumerateDefaultAllPossibleManifests()
        {
            DirectoryPath? currentSearchDirectory = _probStart;
            while (currentSearchDirectory != null)
            {
                var tryManifest = currentSearchDirectory.Value.WithFile(_manifestFilenameConvention);
                yield return tryManifest;
                currentSearchDirectory = currentSearchDirectory.Value.GetParentPathNullable();
            }
        }

        private class SerializableLocalToolsManifest
        {
            [DefaultValue(1)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
            public int version { get; set; }

            [DefaultValue(false)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
            public bool isRoot { get; set; }

            [JsonProperty(Required = Required.Always)]
            // The dictionary's key is the package id
            public Dictionary<string, SerializableLocalToolSinglePackage> tools { get; set; }
        }

        private class SerializableLocalToolSinglePackage
        {
            public string version { get; set; }
            public string[] commands { get; set; }
            public string targetFramework { get; set; }
        }
    }
}
