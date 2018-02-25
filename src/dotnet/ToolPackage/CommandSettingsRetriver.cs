using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.DotNet.Tools;
using Microsoft.Extensions.EnvironmentAbstractions;
using NuGet.ProjectModel;

namespace Microsoft.DotNet.ToolPackage
{
    internal static class CommandSettingsRetriver
    {
        public static IReadOnlyList<CommandSettings> GetCommands(
            string packageId,
            DirectoryPath assetJsonDirectory,
            bool commandsInNuGetCache)
        {
            const string AssetsFileName = "project.assets.json";
            const string ToolSettingsFileName = "DotnetToolSettings.xml";

            try
            {
                var commands = new List<CommandSettings>();
                var lockFile = new LockFileFormat().Read(assetJsonDirectory.WithFile(AssetsFileName).Value);

                DirectoryPath packageDirectory;
                if (commandsInNuGetCache)
                {
                    packageDirectory =
                        new DirectoryPath(lockFile.PackageFolders[0]
                            .Path); //todo no checkin, why there is more than one?
                }
                else
                {
                    packageDirectory = assetJsonDirectory;
                }

                var library = FindLibraryInLockFile(lockFile, packageId);
                var dotnetToolSettings = FindItemInTargetLibrary(library, ToolSettingsFileName);
                if (dotnetToolSettings == null)
                {
                    throw new ToolPackageException(
                        string.Format(
                            CommonLocalizableStrings.ToolPackageMissingSettingsFile,
                            packageId));
                }

                var toolConfigurationPath =
                    packageDirectory
                        .WithSubDirectories(
                            packageId,
                            library.Version.ToNormalizedString())
                        .WithFile(dotnetToolSettings.Path);

                var configuration = ToolConfigurationDeserializer.Deserialize(toolConfigurationPath.Value);

                var entryPointFromLockFile = FindItemInTargetLibrary(library, configuration.ToolAssemblyEntryPoint);
                if (entryPointFromLockFile == null)
                {
                    throw new ToolPackageException(
                        string.Format(
                            CommonLocalizableStrings.ToolPackageMissingEntryPointFile,
                            packageId,
                            configuration.ToolAssemblyEntryPoint));
                }

                commands.Add(new CommandSettings(
                    configuration.CommandName,
                    "dotnet", // Currently only "dotnet" commands are supported
                    packageDirectory
                        .WithSubDirectories(
                            packageId,
                            library.Version.ToNormalizedString())
                        .WithFile(entryPointFromLockFile.Path)));

                return commands;
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
            {
                throw new ToolPackageException(
                    string.Format(
                        CommonLocalizableStrings.FailedToRetrieveToolConfiguration,
                        packageId,
                        ex.Message),
                    ex);
            }
        }

        private static LockFileTargetLibrary FindLibraryInLockFile(LockFile lockFile, string packageId)
        {
            return lockFile
                ?.Targets?.SingleOrDefault(t => t.RuntimeIdentifier != null)
                ?.Libraries?.SingleOrDefault(l => l.Name == packageId);
        }

        private static LockFileItem FindItemInTargetLibrary(LockFileTargetLibrary library, string targetRelativeFilePath)
        {
            return library
                ?.ToolsAssemblies
                ?.SingleOrDefault(t => LockFileMatcher.MatchesFile(t, targetRelativeFilePath));
        }
    }
}
