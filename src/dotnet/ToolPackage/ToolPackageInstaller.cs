using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Configurer;
using Microsoft.DotNet.Tools;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.ToolPackage
{
    internal class ToolPackageInstaller : IToolPackageInstaller
    {
        public const string StagingDirectory = ".stage";

        private readonly IToolPackageStore _store;
        private readonly IProjectRestorer _projectRestorer;
        private readonly FilePath? _tempProject;
        private readonly DirectoryPath _offlineFeed;

        public ToolPackageInstaller(
            IToolPackageStore store,
            IProjectRestorer projectRestorer,
            FilePath? tempProject = null,
            DirectoryPath? offlineFeed = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _projectRestorer = projectRestorer ?? throw new ArgumentNullException(nameof(projectRestorer));
            _tempProject = tempProject;
            _offlineFeed = offlineFeed ?? new DirectoryPath(new CliFolderPathCalculator().CliFallbackFolderPath);
        }

        public IToolPackage InstallPackage(
            NuGetPackageLocation nuGetPackageLocation,
            string targetFramework = null,
            string verbosity = null)
        {
            if (nuGetPackageLocation.PackageId == null)
            {
                throw new ArgumentNullException(nameof(nuGetPackageLocation.PackageId));
            }

            var packageRootDirectory = _store.Root.WithSubDirectories(nuGetPackageLocation.PackageId);
            string rollbackDirectory = null;

            return TransactionalAction.Run<IToolPackage>(
                action: () => {
                    try
                    {

                        var stageDirectory = _store.Root.WithSubDirectories(StagingDirectory, Path.GetRandomFileName());
                        Directory.CreateDirectory(stageDirectory.Value);
                        rollbackDirectory = stageDirectory.Value;

                        ObtainPackage(nuGetPackageLocation, targetFramework, verbosity, stageDirectory, stageDirectory);

                        nuGetPackageLocation.PackageVersion = Path.GetFileName(
                            Directory.EnumerateDirectories(
                                stageDirectory.WithSubDirectories(nuGetPackageLocation.PackageId).Value).Single());

                        var packageDirectory = packageRootDirectory.WithSubDirectories(nuGetPackageLocation.PackageVersion);
                        if (Directory.Exists(packageDirectory.Value))
                        {
                            throw new ToolPackageException(
                                string.Format(
                                    CommonLocalizableStrings.ToolPackageConflictPackageId,
                                    nuGetPackageLocation.PackageId,
                                    nuGetPackageLocation.PackageVersion));
                        }

                        Directory.CreateDirectory(packageRootDirectory.Value);
                        Directory.Move(stageDirectory.Value, packageDirectory.Value);
                        rollbackDirectory = packageDirectory.Value;

                        return new ToolPackageInstance(
                            _store,
                            nuGetPackageLocation.PackageId,
                            nuGetPackageLocation.PackageVersion,
                            packageDirectory);
                    }
                    catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
                    {
                        throw new ToolPackageException(
                            string.Format(
                                CommonLocalizableStrings.FailedToInstallToolPackage,
                                nuGetPackageLocation.PackageId,
                                ex.Message),
                            ex);
                    }
                },
                rollback: () => {
                    if (!string.IsNullOrEmpty(rollbackDirectory) && Directory.Exists(rollbackDirectory))
                    {
                        Directory.Delete(rollbackDirectory, true);
                    }

                    // Delete the root if it is empty
                    if (Directory.Exists(packageRootDirectory.Value) &&
                        !Directory.EnumerateFileSystemEntries(packageRootDirectory.Value).Any())
                    {
                        Directory.Delete(packageRootDirectory.Value, false);
                    }
                });
        }

        public IReadOnlyList<CommandSettings> InstallPackageToNuGetCache(
            NuGetPackageLocation nuGetPackageLocation,
            string targetFramework = null,
            string verbosity = null,
            DirectoryPath? nugetCacheLocation = null)
        {
            var assetJsonOutput = new DirectoryPath(Path.GetTempPath());
            Directory.CreateDirectory(assetJsonOutput.Value);

            ObtainPackage(nuGetPackageLocation, targetFramework, verbosity, assetJsonOutput, nugetCacheLocation);
            return CommandSettingsRetriver.GetCommands(nuGetPackageLocation.PackageId, assetJsonOutput, true);
        }

        private void ObtainPackage(
            NuGetPackageLocation nuGetPackageLocation,
            string targetFramework,
            string verbosity,
            DirectoryPath assetJsonOutput,
            DirectoryPath? outputDirectory = null,
            DirectoryPath? nugetCacheLocation = null
            )
        {
            var tempProject = CreateTempProject(
                packageId: nuGetPackageLocation.PackageId,
                packageVersion: nuGetPackageLocation.PackageVersion,
                targetFramework: targetFramework ?? BundledTargetFramework.GetTargetFrameworkMoniker(),
                restoreDirectory: outputDirectory);

            try
            {
                _projectRestorer.Restore(
                    tempProject,
                    assetJsonOutput,
                    nuGetPackageLocation.NugetConfig,
                    nuGetPackageLocation.Source,
                    verbosity,
                    nugetCacheLocation);
            }
            finally
            {
                File.Delete(tempProject.Value);
            }
        }

        private FilePath CreateTempProject(
            string packageId,
            string packageVersion,
            string targetFramework,
            DirectoryPath? restoreDirectory)
        {
            var tempProject = _tempProject ?? new DirectoryPath(Path.GetTempPath())
                .WithSubDirectories(Path.GetRandomFileName())
                .WithFile(Path.GetRandomFileName() + ".csproj");

            if (Path.GetExtension(tempProject.Value) != "csproj")
            {
                tempProject = new FilePath(Path.ChangeExtension(tempProject.Value, "csproj"));
            }

            Directory.CreateDirectory(tempProject.GetDirectoryPath().Value);

            var tempProjectContent = new XDocument(
                new XElement("Project",
                    new XAttribute("Sdk", "Microsoft.NET.Sdk"),
                    new XElement("PropertyGroup",
                        new XElement("TargetFramework", targetFramework),
                        restoreDirectory.HasValue ? new XElement("RestorePackagesPath", restoreDirectory.Value.Value) : null,
                        new XElement("RestoreProjectStyle", "DotnetToolReference"), // without it, project cannot reference tool package
                        new XElement("RestoreRootConfigDirectory", Directory.GetCurrentDirectory()), // config file probing start directory
                        new XElement("DisableImplicitFrameworkReferences", "true"), // no Microsoft.NETCore.App in tool folder
                        new XElement("RestoreFallbackFolders", "clear"), // do not use fallbackfolder, tool package need to be copied to tool folder
                        new XElement("RestoreAdditionalProjectSources", // use fallbackfolder as feed to enable offline
                            Directory.Exists(_offlineFeed.Value) ? _offlineFeed.Value : string.Empty),
                        new XElement("RestoreAdditionalProjectFallbackFolders", string.Empty), // block other
                        new XElement("RestoreAdditionalProjectFallbackFoldersExcludes", string.Empty),  // block other
                        new XElement("DisableImplicitNuGetFallbackFolder", "true")),  // disable SDK side implicit NuGetFallbackFolder
                     new XElement("ItemGroup",
                        new XElement("PackageReference",
                            new XAttribute("Include", packageId),
                            new XAttribute("Version", packageVersion ?? "*") // nuget will restore * for latest
                            ))
                        ));

            File.WriteAllText(tempProject.Value, tempProjectContent.ToString());
            return tempProject;
        }
    }
}
