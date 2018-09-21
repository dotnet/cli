// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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

        public ToolManifestFinder(DirectoryPath probStart, IFileSystem fileSystem = null)
        {
            _probStart = probStart;
            _fileSystem = fileSystem ?? new FileSystemWrapper();
        }

        public IReadOnlyCollection<ToolManifestFindingResultSinglePackage> Find(FilePath? filePath = null)
        {
            var result = new List<ToolManifestFindingResultSinglePackage>();

            IEnumerable<FilePath> allPossibleManifests =
                filePath != null
                    ? new[] { filePath.Value }
                    : EnumerateDefaultAllPossibleManifests();

            foreach (FilePath possibleManifest in allPossibleManifests)
            {
                if (_fileSystem.File.Exists(possibleManifest.Value))
                {
                    SerializableLocalToolsManifest deserializedManifest = JsonConvert.DeserializeObject<SerializableLocalToolsManifest>(
                        _fileSystem.File.ReadAllText(possibleManifest.Value), new JsonSerializerSettings
                        {
                            MissingMemberHandling = MissingMemberHandling.Ignore
                        });

                    var errors = new List<string>();

                    if (!deserializedManifest.isRoot)
                    {
                        errors.Add(LocalizableStrings.IsRootFalseNotSupported);
                    }

                    if (deserializedManifest.version != 1)
                    {
                        errors.Add(LocalizableStrings.Version1NotSupported);
                    }

                    foreach (var tools in deserializedManifest.tools)
                    {
                        var packageLevelErrors = new List<string>();
                        var packageIdString = tools.Key;
                        var packageId = new PackageId(packageIdString);

                        string versionString = tools.Value.version;
                        NuGetVersion version = null;
                        if (versionString is null)
                        {
                            packageLevelErrors.Add(LocalizableStrings.MissingVersion);
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

                        if (tools.Value.commands != null)
                        {
                            if (tools.Value.commands.Length == 0)
                            {
                                packageLevelErrors.Add(LocalizableStrings.FieldCommandsIsMissing);
                            }
                        }
                        else
                        {
                            packageLevelErrors.Add(LocalizableStrings.FieldCommandsIsMissing);
                        }

                        if (packageLevelErrors.Any())
                        {
                            var joined = string.Join(", ", packageLevelErrors);
                            errors.Add(string.Format(LocalizableStrings.PackageNameAndErrors, packageId.ToString(), joined));
                        }
                        else
                        {
                            result.Add(new ToolManifestFindingResultSinglePackage(
                                packageId,
                                version,
                                ToolCommandName.Convert(tools.Value.commands),
                                targetFramework));
                        }
                    }

                    if (errors.Any())
                    {
                        throw new ToolManifestException(string.Format(LocalizableStrings.InvalidManifestFilePrefix,
                            string.Join(" ", errors)));
                    }

                    return result;
                }
            }

            throw new ToolManifestException(
                string.Format(LocalizableStrings.CannotFindAnyManifestsFileSearched,
                    string.Join("; ", allPossibleManifests.Select(f => f.Value))));
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
            [JsonProperty(Required = Required.Always)]
            public int version { get; set; }

            [JsonProperty(Required = Required.Always)]
            public bool isRoot { get; set; }

            [JsonProperty(Required = Required.Always)]
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
