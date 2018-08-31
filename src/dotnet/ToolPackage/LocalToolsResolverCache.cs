using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.ToolPackage;
using Microsoft.Extensions.EnvironmentAbstractions;
using Newtonsoft.Json;
using NuGet.Frameworks;
using NuGet.Versioning;

namespace Microsoft.DotNet.ToolPackage
{
    internal class LocalToolsResolverCache
    {
        private readonly DirectoryPath _cacheVersionedDirectory;
        private readonly IFileSystem _fileSystem;

        public LocalToolsResolverCache(IFileSystem fileSystem, DirectoryPath cacheDirectory, int version)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _cacheVersionedDirectory = cacheDirectory.WithSubDirectories(version.ToString());
        }

        public void Save(
            PackageId packageId,
            NuGetVersion version,
            NuGetFramework targetFramework,
            string runtimeIdentifier,
            IReadOnlyList<CommandSettings> listOfCommandSettings,
            DirectoryPath nuGetGlobalPackagesFolder)
        {
            EnsureFileStorageExists();
            CacheRow serializableSchema = Convert(
                version,
                targetFramework,
                runtimeIdentifier,
                listOfCommandSettings,
                nuGetGlobalPackagesFolder);

            string json = JsonConvert.SerializeObject(new[] {serializableSchema});
            _fileSystem.File.WriteAllText(GetCacheFile(packageId), json);
        }

        private string GetCacheFile(PackageId packageId)
        {
            return _cacheVersionedDirectory.WithFile(packageId.ToString()).Value;
        }

        private void EnsureFileStorageExists()
        {
            _fileSystem.Directory.CreateDirectory(_cacheVersionedDirectory.Value);
        }

        private static CacheRow Convert(NuGetVersion version,
            NuGetFramework targetFramework,
            string runtimeIdentifier,
            IReadOnlyList<CommandSettings> listOfCommandSettings,
            DirectoryPath nuGetGlobalPackagesFolder)
        {
            return new CacheRow
            {
                Version = version.ToNormalizedString(),
                TargetFramework = targetFramework.GetShortFolderName(),
                RuntimeIdentifier = runtimeIdentifier,
                SerializableCommandSettingsArray =
                    listOfCommandSettings.Select(s => new SerializableCommandSettings
                    {
                        Name = s.Name,
                        Runner = s.Runner,
                        RelativeToNuGetGlobalPackagesFolderPathToDll =
                            Path.GetRelativePath(nuGetGlobalPackagesFolder.Value, s.Executable.Value)
                    }).ToArray()
            };
        }

        public IReadOnlyList<CommandSettings> Load(
            PackageId packageId,
            NuGetVersion version,
            NuGetFramework targetFramework,
            string runtimeIdentifier,
            DirectoryPath nuGetGlobalPackagesFolder)
        {
            string packageCacheFile = GetCacheFile(packageId);
            if (_fileSystem.File.Exists(packageCacheFile))
            {
                CacheRow[] cacheTable =
                    JsonConvert.DeserializeObject<CacheRow[]>(_fileSystem.File.ReadAllText(packageCacheFile));

                SerializableCommandSettings[] matchingCommandSettingsArray =
                    cacheTable
                        .SingleOrDefault(row => row.Version == version.ToNormalizedString() &&
                                                row.TargetFramework == targetFramework.GetShortFolderName() &&
                                                row.RuntimeIdentifier == runtimeIdentifier)
                        ?.SerializableCommandSettingsArray;

                if (matchingCommandSettingsArray != null)
                {
                    return matchingCommandSettingsArray.Select(
                        serializableCommandSettings =>
                            new CommandSettings(
                                serializableCommandSettings.Name,
                                serializableCommandSettings.Runner,
                                nuGetGlobalPackagesFolder.WithFile(serializableCommandSettings
                                    .RelativeToNuGetGlobalPackagesFolderPathToDll))
                    ).ToArray();
                }
            }

            return Array.Empty<CommandSettings>();
        }

        private class CacheRow
        {
            public string Version { get; set; }
            public string TargetFramework { get; set; }
            public string RuntimeIdentifier { get; set; }
            public SerializableCommandSettings[] SerializableCommandSettingsArray { get; set; }
        }

        private class SerializableCommandSettings
        {
            public string Name { get; set; }
            public string Runner { get; set; }
            public string RelativeToNuGetGlobalPackagesFolderPathToDll { get; set; }
        }
    }
}
