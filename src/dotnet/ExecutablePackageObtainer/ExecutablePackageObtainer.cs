using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.ExecutablePackageObtainer
{
    public class ExecutablePackageObtainer
    {

        private readonly Lazy<string> _bundledTargetFrameworkMoniker;
        private readonly Func<FilePath> _getTempProjectPath;
        private readonly ICanAddPackageToProjectFile _packageToProjectFileAdder;
        private readonly ICanRestoreProject _projectRestorer;
        private readonly DirectoryPath _toolsPath;

        public ExecutablePackageObtainer(
            DirectoryPath toolsPath,
            Func<FilePath> getTempProjectPath,
            Lazy<string> bundledTargetFrameworkMoniker,
            ICanAddPackageToProjectFile packageToProjectFileAdder,
            ICanRestoreProject projectRestorer
        )
        {
            _getTempProjectPath = getTempProjectPath;
            _bundledTargetFrameworkMoniker = bundledTargetFrameworkMoniker;
            _projectRestorer = projectRestorer ?? throw new ArgumentNullException(nameof(projectRestorer));
            _packageToProjectFileAdder = packageToProjectFileAdder ??
                                         throw new ArgumentNullException(nameof(packageToProjectFileAdder));
            _toolsPath = toolsPath ?? throw new ArgumentNullException(nameof(toolsPath));
        }

        public ToolConfigurationAndExecutableDirectory ObtainAndReturnExecutablePath(
            string packageId,
            string packageVersion = null,
            FilePath nugetconfig = null,
            string targetframework = null)
        {
            if (packageId == null) throw new ArgumentNullException(nameof(packageId));
            if (targetframework == null)
            {
                targetframework = _bundledTargetFrameworkMoniker.Value;
            }

            var packageVersionOrPlaceHolder = new PackageVersion(packageVersion);

            DirectoryPath individualToolVersion =
                CreateIndividualToolVersionDirectory(packageId, packageVersionOrPlaceHolder);

            FilePath tempProjectPath = CreateTempProject(
                packageId,
                packageVersionOrPlaceHolder,
                targetframework,
                individualToolVersion);

            if (packageVersionOrPlaceHolder.IsPlaceHolder)
            {
                InvokeAddPackageRestore(
                    nugetconfig,
                    tempProjectPath,
                    packageId);
            }

            InvokeRestore(nugetconfig, tempProjectPath, individualToolVersion);

            if (packageVersionOrPlaceHolder.IsPlaceHolder)
            {
                var concreteVersion =
                    new DirectoryInfo(
                        Directory.GetDirectories(
                            individualToolVersion.WithCombineFollowing(packageId).Value).Single()).Name;
                DirectoryPath concreteVersionIndividualToolVersion =
                    individualToolVersion.GetParentPath().WithCombineFollowing(concreteVersion);
                Directory.Move(individualToolVersion.Value, concreteVersionIndividualToolVersion.Value);

                individualToolVersion = concreteVersionIndividualToolVersion;
                packageVersion = concreteVersion;
            }

            ToolConfiguration toolConfiguration = GetConfiguration(packageId, packageVersion, individualToolVersion);

            return new ToolConfigurationAndExecutableDirectory(
                toolConfiguration,
                individualToolVersion.WithCombineFollowing(
                    packageId,
                    packageVersion,
                    "tools",
                    targetframework));
        }

        private static ToolConfiguration GetConfiguration(
            string packageId,
            string packageVersion,
            DirectoryPath individualToolVersion)
        {
            FilePath toolConfigurationPath =
                individualToolVersion
                    .WithCombineFollowing(packageId, packageVersion, "tools")
                    .CreateFilePathWithCombineFollowing("DotnetToolsConfig.xml");

            ToolConfiguration toolConfiguration =
                ToolConfigurationDeserializer.Deserialize(toolConfigurationPath.Value);
            return toolConfiguration;
        }

        private void InvokeRestore(
            FilePath nugetconfig,
            FilePath tempProjectPath,
            DirectoryPath individualToolVersion)
        {
            _projectRestorer.Restore(tempProjectPath, individualToolVersion, nugetconfig);
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
            if (packageVersion.IsConcreteValue)
            {
                File.WriteAllText(tempProjectPath.Value,
                    string.Format(
                        TemporaryProjectTemplate,
                        targetframework,
                        individualToolVersion.Value,
                        packageId,
                        packageVersion.Value));
            }
            else
            {
                File.WriteAllText(tempProjectPath.Value,
                    string.Format(
                        TemporaryProjectTemplateWithoutPackage,
                        targetframework,
                        individualToolVersion.Value));
            }
            return tempProjectPath;
        }

        private void InvokeAddPackageRestore(
            FilePath nugetconfig,
            FilePath tempProjectPath,
            string packageId)
        {
            if (nugetconfig != null)
            {
                File.Copy(
                    nugetconfig.Value,
                    tempProjectPath
                        .GetDirectoryPath()
                        .CreateFilePathWithCombineFollowing("nuget.config")
                        .Value);
            }

            _packageToProjectFileAdder.Add(tempProjectPath, packageId);
        }

        private DirectoryPath CreateIndividualToolVersionDirectory(
            string packageId,
            PackageVersion packageVersion)
        {
            DirectoryPath individualTool = _toolsPath.WithCombineFollowing(packageId);
            DirectoryPath individualToolVersion = individualTool.WithCombineFollowing(packageVersion.Value);
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
        
        private const string TemporaryProjectTemplate = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>{0}</TargetFramework>
    <RestorePackagesPath>{1}</RestorePackagesPath>
    <DisableImplicitFrameworkReferences>true</DisableImplicitFrameworkReferences>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""{2}"" Version=""{3}""/>
  </ItemGroup>
</Project>";

        private const string TemporaryProjectTemplateWithoutPackage = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>{0}</TargetFramework>
    <RestorePackagesPath>{1}</RestorePackagesPath>
    <DisableImplicitFrameworkReferences>true</DisableImplicitFrameworkReferences>
  </PropertyGroup>
</Project>";
    }
}
