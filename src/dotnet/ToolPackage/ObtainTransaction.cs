using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.DotNet.Tools;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.Extensions.EnvironmentAbstractions;
using NuGet.ProjectModel;
using System.Transactions;

namespace Microsoft.DotNet.ToolPackage
{
    internal class ObtainTransaction : IObtainTransaction
    {
        private string _packageId;
        private string _packageVersion;
        private FilePath? _nugetconfig;
        private string _targetframework;
        private string _source;

        private Lazy<string> _bundledTargetFrameworkMoniker;
        private Func<FilePath> _getTempProjectPath;
        private IProjectRestorer _projectRestorer;
        private DirectoryPath _toolsPath;
        private DirectoryPath _offlineFeedPath;
        private DirectoryPath _stageDirectory;

        internal ObtainTransaction(
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
            _packageId = packageId;
            _packageVersion = packageVersion;
            _nugetconfig = nugetconfig;
            _targetframework = targetframework;
            _source = source;

            _bundledTargetFrameworkMoniker = bundledTargetFrameworkMoniker;
            _getTempProjectPath = getTempProjectPath;
            _projectRestorer = projectRestorer;
            _toolsPath = toolsPath;
            _offlineFeedPath = offlineFeedPath;
            _stageDirectory = _toolsPath.WithSubDirectories(".stage", Path.GetRandomFileName());
        }

        public ToolConfigurationAndExecutablePath ObtainAndReturnExecutablePath()
        {
            if (_packageId == null)
            {
                throw new ArgumentNullException(nameof(_packageId));
            }

            if (_nugetconfig != null)
            {
                if (!File.Exists(_nugetconfig.Value.Value))
                {
                    throw new PackageObtainException(
                        string.Format(CommonLocalizableStrings.NuGetConfigurationFileDoesNotExist,
                            Path.GetFullPath(_nugetconfig.Value.Value)));
                }
            }

            if (_targetframework == null)
            {
                _targetframework = _bundledTargetFrameworkMoniker.Value;
            }

            var packageVersionOrPlaceHolder = new PackageVersion(_packageVersion);

            DirectoryPath toolDirectory =
                CreateIndividualToolVersionDirectory(packageVersionOrPlaceHolder);

            FilePath tempProjectPath = CreateTempProject(
                packageVersionOrPlaceHolder,
                toolDirectory);

            _projectRestorer.Restore(tempProjectPath, toolDirectory, _nugetconfig, _source);

            if (packageVersionOrPlaceHolder.IsPlaceholder)
            {
                var concreteVersion =
                    new DirectoryInfo(
                        Directory.GetDirectories(
                            toolDirectory.WithSubDirectories(_packageId).Value).Single()).Name;
                DirectoryPath versioned =
                    toolDirectory.GetParentPath().WithSubDirectories(concreteVersion);

                MoveToVersionedDirectory(versioned, toolDirectory);

                toolDirectory = versioned;
                _packageVersion = concreteVersion;
            }

            LockFile lockFile = new LockFileFormat()
                .ReadWithLock(toolDirectory.WithFile("project.assets.json").Value)
                .Result;

            LockFileItem dotnetToolSettings = FindAssetInLockFile(lockFile, "DotnetToolSettings.xml");

            if (dotnetToolSettings == null)
            {
                throw new PackageObtainException(
                    string.Format(CommonLocalizableStrings.ToolPackageMissingSettingsFile, _packageId));
            }

            FilePath toolConfigurationPath =
                toolDirectory
                    .WithSubDirectories(_packageId, _packageVersion)
                    .WithFile(dotnetToolSettings.Path);

            ToolConfiguration toolConfiguration =
                ToolConfigurationDeserializer.Deserialize(toolConfigurationPath.Value);

            var entryPointFromLockFile =
                FindAssetInLockFile(lockFile, toolConfiguration.ToolAssemblyEntryPoint);

            if (entryPointFromLockFile == null)
            {
                throw new PackageObtainException(string.Format(CommonLocalizableStrings.ToolPackageMissingEntryPointFile,
                    _packageId, toolConfiguration.ToolAssemblyEntryPoint));
            }

            return new ToolConfigurationAndExecutablePath(
                toolConfiguration,
                _toolsPath.WithSubDirectories(
                        _packageId,
                        _packageVersion,
                        _packageId,
                        _packageVersion)
                    .WithFile(entryPointFromLockFile.Path));
        }

        public void Commit(Enlistment enlistment)
        {
            Directory.Move(
                _stageDirectory.WithSubDirectories(_packageId).Value,
                _toolsPath.WithSubDirectories(_packageId).Value);

            Directory.Delete(_stageDirectory.Value, true);
        }

        public void InDoubt(Enlistment enlistment)
        {
            Rollback(enlistment);
        }

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            if (Directory.Exists(_toolsPath.WithSubDirectories(_packageId).Value))
            {
                preparingEnlistment.ForceRollback();
                throw new PackageObtainException($"A tool with the same PackageId {_packageId} existed."); // TODO loc no checkin
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

        private DirectoryPath CreateIndividualToolVersionDirectory(
            PackageVersion packageVersion)
        {
            DirectoryPath individualTool = _stageDirectory.WithSubDirectories(_packageId);
            DirectoryPath individualToolVersion = individualTool.WithSubDirectories(packageVersion.Value);
            EnsureDirectoryExists(individualToolVersion);
            return individualToolVersion;
        }

        private LockFileItem FindAssetInLockFile(
         LockFile lockFile,
         string targetRelativeFilePath)
        {
            return lockFile
                .Targets?.SingleOrDefault(t => t.RuntimeIdentifier != null)
                ?.Libraries?.SingleOrDefault(l => l.Name == _packageId)
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
            PackageVersion packageVersion,
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
                        new XElement("TargetFramework", _targetframework),
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
                            new XAttribute("Include", _packageId),
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
    }
}
