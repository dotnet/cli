using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.DotNet.Tools;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Configurer;
using Microsoft.Extensions.EnvironmentAbstractions;
using NuGet.ProjectModel;
using System.Transactions;

namespace Microsoft.DotNet.ToolPackage
{
    internal class ObtainAndReturnExecutablePathtransactional: IEnlistmentNotification
    {
        private string packageId;
        private string packageVersion;
        private FilePath? nugetconfig;
        private string targetframework;
        private string source;
        private Lazy<string> _bundledTargetFrameworkMoniker;
        private Func<FilePath> _getTempProjectPath;
        private IProjectRestorer _projectRestorer;
        private DirectoryPath _toolsPath;
        private DirectoryPath _offlineFeedPath;
        private DirectoryPath _stageDirectory;

        public ObtainAndReturnExecutablePathtransactional(
            string packageId,
            string packageVersion,
            FilePath? nugetconfig,
            string targetframework,
            string source,
            Lazy<string> bundledTargetFrameworkMoniker,
            Func<FilePath> getTempProjectPath,
            IProjectRestorer projectRestorer,
            DirectoryPath toolsPath,
            DirectoryPath offlineFeedPath)
        {
            this.packageId = packageId;
            this.packageVersion = packageVersion;
            this.nugetconfig = nugetconfig;
            this.targetframework = targetframework;
            this.source = source;

            _bundledTargetFrameworkMoniker = bundledTargetFrameworkMoniker;
            _getTempProjectPath = getTempProjectPath;
            _projectRestorer = projectRestorer;
            _toolsPath = toolsPath;
            _offlineFeedPath = offlineFeedPath;
            _stageDirectory = _toolsPath.WithSubDirectories(".stage", Path.GetRandomFileName());
        }

        private DirectoryPath StageDirectory => _toolsPath.WithSubDirectories(".stage", Path.GetRandomFileName());

        public ToolConfigurationAndExecutablePath Obtain()
        {
            if (packageId == null)
            {
                throw new ArgumentNullException(nameof(packageId));
            }

            if (nugetconfig != null)
            {
                if (!File.Exists(nugetconfig.Value.Value))
                {
                    throw new PackageObtainException(
                        string.Format(CommonLocalizableStrings.NuGetConfigurationFileDoesNotExist,
                            Path.GetFullPath(nugetconfig.Value.Value)));
                }
            }

            if (targetframework == null)
            {
                targetframework = _bundledTargetFrameworkMoniker.Value;
            }

            var packageVersionOrPlaceHolder = new PackageVersion(packageVersion);

            DirectoryPath toolDirectory =
                CreateIndividualToolVersionDirectory(packageId, packageVersionOrPlaceHolder);

            FilePath tempProjectPath = CreateTempProject(
                packageId,
                packageVersionOrPlaceHolder,
                targetframework,
                toolDirectory);

            _projectRestorer.Restore(tempProjectPath, toolDirectory, nugetconfig, source);

            if (packageVersionOrPlaceHolder.IsPlaceholder)
            {
                var concreteVersion =
                    new DirectoryInfo(
                        Directory.GetDirectories(
                            toolDirectory.WithSubDirectories(packageId).Value).Single()).Name;
                DirectoryPath versioned =
                    toolDirectory.GetParentPath().WithSubDirectories(concreteVersion);

                MoveToVersionedDirectory(versioned, toolDirectory);

                toolDirectory = versioned;
                packageVersion = concreteVersion;
            }

            LockFile lockFile = new LockFileFormat()
                .ReadWithLock(toolDirectory.WithFile("project.assets.json").Value)
                .Result;

            LockFileItem dotnetToolSettings = FindAssetInLockFile(lockFile, "DotnetToolSettings.xml", packageId);

            if (dotnetToolSettings == null)
            {
                throw new PackageObtainException(
                    string.Format(CommonLocalizableStrings.ToolPackageMissingSettingsFile, packageId));
            }

            FilePath toolConfigurationPath =
                toolDirectory
                    .WithSubDirectories(packageId, packageVersion)
                    .WithFile(dotnetToolSettings.Path);

            ToolConfiguration toolConfiguration =
                ToolConfigurationDeserializer.Deserialize(toolConfigurationPath.Value);

            var entryPointFromLockFile =
                FindAssetInLockFile(lockFile, toolConfiguration.ToolAssemblyEntryPoint, packageId);

            if (entryPointFromLockFile == null)
            {
                throw new PackageObtainException(string.Format(CommonLocalizableStrings.ToolPackageMissingEntryPointFile,
                    packageId, toolConfiguration.ToolAssemblyEntryPoint));
            }

            return new ToolConfigurationAndExecutablePath(
                toolConfiguration,
                toolDirectory.WithSubDirectories(
                        packageId,
                        packageVersion)
                    .WithFile(entryPointFromLockFile.Path));
        }

        private DirectoryPath CreateIndividualToolVersionDirectory(
            string packageId,
            PackageVersion packageVersion)
        {
            DirectoryPath individualTool = _stageDirectory.WithSubDirectories(packageId);
            DirectoryPath individualToolVersion = individualTool.WithSubDirectories(packageVersion.Value);
            EnsureDirectoryExists(individualToolVersion);
            return individualToolVersion;
        }

        private static LockFileItem FindAssetInLockFile(
         LockFile lockFile,
         string targetRelativeFilePath, string packageId)
        {
            return lockFile
                .Targets?.SingleOrDefault(t => t.RuntimeIdentifier != null)
                ?.Libraries?.SingleOrDefault(l => l.Name == packageId)
                ?.ToolsAssemblies
                ?.SingleOrDefault(t => LockFileMatcher.MatchesFile(t, targetRelativeFilePath));
        }

        private static void MoveToVersionedDirectory(
            DirectoryPath versioned,
            DirectoryPath temporary)
        {
            if (Directory.Exists(versioned.Value))
            {
                Directory.Delete(versioned.Value, recursive: true);
            }

            Directory.Move(temporary.Value, versioned.Value);
        }

        private FilePath CreateTempProject(
            string packageId,
            PackageVersion packageVersion,
            string targetframework,
            DirectoryPath individualToolVersion)
        {
            FilePath tempProjectPath = _getTempProjectPath();
            if (Path.GetExtension(tempProjectPath.Value) != "csproj")
            {
                tempProjectPath = new FilePath(Path.ChangeExtension(tempProjectPath.Value, "csproj"));
            }

            EnsureDirectoryExists(tempProjectPath.GetDirectoryPath());
            var tempProjectContent = new XDocument(
                new XElement("Project",
                    new XAttribute("Sdk", "Microsoft.NET.Sdk"),
                    new XElement("PropertyGroup",
                        new XElement("TargetFramework", targetframework),
                        new XElement("RestorePackagesPath", individualToolVersion.Value), // tool package will restore to tool folder
                        new XElement("RestoreProjectStyle", "DotnetToolReference"), // without it, project cannot reference tool package
                        new XElement("RestoreRootConfigDirectory", Directory.GetCurrentDirectory()), // config file probing start directory
                        new XElement("DisableImplicitFrameworkReferences", "true"), // no Microsoft.NETCore.App in tool folder
                        new XElement("RestoreFallbackFolders", "clear"), // do not use fallbackfolder, tool package need to be copied to tool folder
                        new XElement("RestoreAdditionalProjectSources", // use fallbackfolder as feed to enable offline
                            Directory.Exists(_offlineFeedPath.Value) ? _offlineFeedPath.Value : string.Empty),
                        new XElement("RestoreAdditionalProjectFallbackFolders", string.Empty), // block other
                        new XElement("RestoreAdditionalProjectFallbackFoldersExcludes", string.Empty),  // block other
                        new XElement("DisableImplicitNuGetFallbackFolder", "true")),  // disable SDK side implicit NuGetFallbackFolder
                     new XElement("ItemGroup",
                        new XElement("PackageReference",
                            new XAttribute("Include", packageId),
                            new XAttribute("Version", packageVersion.IsConcreteValue ? packageVersion.Value : "*") // nuget will restore * for latest
                            ))
                        ));

            File.WriteAllText(tempProjectPath.Value,
                tempProjectContent.ToString());

            return tempProjectPath;
        }

        private static void EnsureDirectoryExists(DirectoryPath path)
        {
            if (!Directory.Exists(path.Value))
            {
                Directory.CreateDirectory(path.Value);
            }
        }

        public void Commit(Enlistment enlistment)
        {
            Directory.Move(
                _stageDirectory.WithSubDirectories(packageId).Value,
                _toolsPath.WithSubDirectories(packageId).Value);

            Directory.Delete(_stageDirectory.Value, true);
        }

        public void InDoubt(Enlistment enlistment)
        {
            Rollback(enlistment);
        }

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            if (Directory.Exists(_toolsPath.WithSubDirectories(packageId).Value))
            {
                preparingEnlistment.ForceRollback();
                throw new PackageObtainException("A tool with the same name existed."); // TODO loc no checkin
            }

            preparingEnlistment.Prepared();
        }

        public void Rollback(Enlistment enlistment)
        {
            if (Directory.Exists(_stageDirectory.Value))
            {
                Directory.Delete(_stageDirectory.Value, recursive: true);
            }

            enlistment.Done();
        }
    }
}
