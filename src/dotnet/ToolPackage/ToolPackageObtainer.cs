﻿using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.DotNet.Tools;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.Extensions.EnvironmentAbstractions;
using NuGet.ProjectModel;

namespace Microsoft.DotNet.ToolPackage
{
    internal class ToolPackageObtainer
    {
        private readonly Lazy<string> _bundledTargetFrameworkMoniker;
        private readonly Func<FilePath> _getTempProjectPath;
        private readonly IPackageToProjectFileAdder _packageToProjectFileAdder;
        private readonly IProjectRestorer _projectRestorer;
        private readonly DirectoryPath _toolsPath;

        public ToolPackageObtainer(
            DirectoryPath toolsPath,
            Func<FilePath> getTempProjectPath,
            Lazy<string> bundledTargetFrameworkMoniker,
            IPackageToProjectFileAdder packageToProjectFileAdder,
            IProjectRestorer projectRestorer
        )
        {
            _getTempProjectPath = getTempProjectPath;
            _bundledTargetFrameworkMoniker = bundledTargetFrameworkMoniker;
            _projectRestorer = projectRestorer ?? throw new ArgumentNullException(nameof(projectRestorer));
            _packageToProjectFileAdder = packageToProjectFileAdder ??
                                         throw new ArgumentNullException(nameof(packageToProjectFileAdder));
            _toolsPath = toolsPath;
        }

        public ToolConfigurationAndExecutablePath ObtainAndReturnExecutablePath(
            string packageId,
            string packageVersion = null,
            FilePath? nugetconfig = null,
            string targetframework = null,
            string source = null)
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

            if (packageVersionOrPlaceHolder.IsPlaceholder)
            {
                InvokeAddPackageRestore(
                    nugetconfig,
                    tempProjectPath,
                    packageId);
            }

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
                throw new PackageObtainException("Cannot find DotnetToolSettings");
            }

            FilePath toolConfigurationPath =
                toolDirectory
                    .WithSubDirectories(packageId, packageVersion)
                    .WithFile(dotnetToolSettings.Path);

            ToolConfiguration toolConfiguration =
                ToolConfigurationDeserializer.Deserialize(toolConfigurationPath.Value);

            var entryPointFromLockFile = FindAssetInLockFile(lockFile, toolConfiguration.ToolAssemblyEntryPoint, packageId);

            if (entryPointFromLockFile == null)
            {
                throw new PackageObtainException("Cannot find tool entry point the package");
            }

            return new ToolConfigurationAndExecutablePath(
                toolConfiguration,
                toolDirectory.WithSubDirectories(
                    packageId,
                    packageVersion)
                .WithFile(entryPointFromLockFile.Path));
        }

        private static LockFileItem FindAssetInLockFile(LockFile lockFile, string toolConfigurationToolAssemblyEntryPoint, string packageId)
        {
            return lockFile
                .Targets?.SingleOrDefault(t => t.RuntimeIdentifier != null)
                ?.Libraries?.SingleOrDefault(l => l.Name == packageId)
                ?.ToolsAssemblies
                ?.SingleOrDefault(t => LockFileMatcher.MatchesFile(t.Path, toolConfigurationToolAssemblyEntryPoint));
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
                        new XElement("RestorePackagesPath", individualToolVersion.Value),
                        new XElement("RestoreProjectStyle", "DotnetToolReference"),
                        new XElement("RestoreRootConfigDirectory", Directory.GetCurrentDirectory()),
                        new XElement("DisableImplicitFrameworkReferences", "true")
                    ),
                    packageVersion.IsConcreteValue
                        ? new XElement("ItemGroup",
                            new XElement("PackageReference",
                                new XAttribute("Include", packageId),
                                new XAttribute("Version", packageVersion.Value)
                            ))
                        : null));

            File.WriteAllText(tempProjectPath.Value,
                tempProjectContent.ToString());

            return tempProjectPath;
        }

        private void InvokeAddPackageRestore(
            FilePath? nugetconfig,
            FilePath tempProjectPath,
            string packageId)
        {
            if (nugetconfig != null)
            {
                File.Copy(
                    nugetconfig.Value.Value,
                    tempProjectPath
                        .GetDirectoryPath()
                        .WithFile("nuget.config")
                        .Value);
            }

            _packageToProjectFileAdder.Add(tempProjectPath, packageId);
        }

        private DirectoryPath CreateIndividualToolVersionDirectory(
            string packageId,
            PackageVersion packageVersion)
        {
            DirectoryPath individualTool = _toolsPath.WithSubDirectories(packageId);
            DirectoryPath individualToolVersion = individualTool.WithSubDirectories(packageVersion.Value);
            EnsureDirectoryExists(individualToolVersion);
            return individualToolVersion;
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
