using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.ProjectModel.Graph;
using Microsoft.DotNet.ProjectModel.Compilation;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.PlatformAbstractions;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.ProjectModel;

using LockFile = Microsoft.DotNet.ProjectModel.Graph.LockFile;
using FileFormatException = Microsoft.DotNet.ProjectModel.FileFormatException;

namespace Microsoft.DotNet.Cli.Utils
{
    public class ProjectToolsCommandResolver : ICommandResolver
    {
        private static readonly NuGetFramework s_toolPackageFramework = FrameworkConstants.CommonFrameworks.NetStandardApp15;
        
        private static readonly CommandResolutionStrategy s_commandResolutionStrategy = 
            CommandResolutionStrategy.ProjectToolsPackage;

        private static readonly string s_currentRuntimeIdentifier = PlatformServices.Default.Runtime.GetLegacyRestoreRuntimeIdentifier();


        private List<string> _allowedCommandExtensions;
        private IPackagedCommandSpecFactory _packagedCommandSpecFactory;

        public ProjectToolsCommandResolver(IPackagedCommandSpecFactory packagedCommandSpecFactory)
        {
            _packagedCommandSpecFactory = packagedCommandSpecFactory;

            _allowedCommandExtensions = new List<string>() 
            {
                FileNameSuffixes.DotNet.DynamicLib
            };
        }

        public CommandSpec Resolve(CommandResolverArguments commandResolverArguments)
        {
            if (commandResolverArguments.CommandName == null
                || commandResolverArguments.ProjectDirectory == null)
            {
                return null;
            }
            
            return ResolveFromProjectTools(
                commandResolverArguments.CommandName, 
                commandResolverArguments.CommandArguments.OrEmptyIfNull(),
                commandResolverArguments.ProjectDirectory);


        }

        private CommandSpec ResolveFromProjectTools(
            string commandName, 
            IEnumerable<string> args,
            string projectDirectory)
        {
            var projectContext = GetProjectContextFromDirectoryForFirstTarget(projectDirectory);

            if (projectContext == null)
            {
                return null;
            }

            var toolsLibraries = projectContext.ProjectFile.Tools.OrEmptyIfNull();

            return ResolveCommandSpecFromAllToolLibraries(
                toolsLibraries,
                commandName, 
                args,
                projectContext);
        }

        private CommandSpec ResolveCommandSpecFromAllToolLibraries(
            IEnumerable<LibraryRange> toolsLibraries,
            string commandName,
            IEnumerable<string> args,
            ProjectContext projectContext)
        {
            foreach (var toolLibrary in toolsLibraries)
            {
                var commandSpec = ResolveCommandSpecFromToolLibrary(toolLibrary, commandName, args, projectContext);

                if (commandSpec != null)
                {
                    return commandSpec;
                }
            }

            return null;
        }

        private CommandSpec ResolveCommandSpecFromToolLibrary(
            LibraryRange toolLibrary,
            string commandName,
            IEnumerable<string> args,
            ProjectContext projectContext)
        {
            var nugetPackagesRoot = projectContext.PackagesDirectory;

            var lockFile = GetToolLockFile(toolLibrary, nugetPackagesRoot);
            var lockFilePackageLibrary = lockFile.PackageLibraries.FirstOrDefault(l => l.Name == toolLibrary.Name);

            var toolHiveRoot = Path.GetDirectoryName(lockFile.LockFilePath);
            var depsFilePath = GetToolDepsFilePath(toolLibrary, lockFile, toolHiveRoot);
            
            var toolProjectContext = new ProjectContextBuilder()
                    .WithLockFile(lockFile)
                    .WithTargetFramework(s_toolPackageFramework.ToString())
                    .Build();

            var exporter = toolProjectContext.CreateExporter(Constants.DefaultConfiguration);

            var commandSpec = _packagedCommandSpecFactory.CreateCommandSpecFromLibrary(
                    lockFilePackageLibrary,
                    commandName,
                    args,
                    _allowedCommandExtensions,
                    projectContext.PackagesDirectory,
                    s_commandResolutionStrategy,
                    depsFilePath);

            if (commandSpec == null)
            {
                return null;
            }

            // So an tool can access it's runtime config via AppContext.BaseDirectory
            var commandPath = GetCommandPath(
                lockFilePackageLibrary,
                commandName,
                _allowedCommandExtensions,
                nugetPackagesRoot);

            var runtimeConfigPath = GetToolRuntimeConfigFilePath(commandPath);
            CopyRuntimeConfigToToolHive(toolHiveRoot, runtimeConfigPath);

            return commandSpec;
        }

        private LockFile GetToolLockFile(
            LibraryRange toolLibrary,
            string nugetPackagesRoot)
        {
            var lockFilePath = GetToolLockFilePath(toolLibrary, nugetPackagesRoot);

            if (!File.Exists(lockFilePath))
            {
                return null;
            }

            LockFile lockFile = null;

            try
            {
                lockFile = LockFileReader.Read(lockFilePath);
            }
            catch (FileFormatException ex)
            {
                throw ex;
            }

            return lockFile;
        }

        private string GetToolLockFilePath(
            LibraryRange toolLibrary,
            string nugetPackagesRoot)
        {
            var toolPathCalculator = new ToolPathCalculator(nugetPackagesRoot);

            return toolPathCalculator.GetBestLockFilePath(
                toolLibrary.Name, 
                toolLibrary.VersionRange, 
                s_toolPackageFramework);
        }

        private ProjectContext GetProjectContextFromDirectoryForFirstTarget(string projectRootPath)
        {
            if (projectRootPath == null)
            {
                return null;
            }

            if (!File.Exists(Path.Combine(projectRootPath, Project.FileName)))
            {
                return null;
            }

            var projectContext = ProjectContext.CreateContextForEachTarget(projectRootPath).FirstOrDefault();

            return projectContext;
        }

        private string GetToolDepsFilePath(
            LibraryRange toolLibrary, 
            LockFile toolLockFile, 
            string depsPathRoot)
        {
            var depsJsonPath = Path.Combine(
                depsPathRoot,
                toolLibrary.Name + FileNameSuffixes.DepsJson);

            EnsureToolJsonDepsFileExists(toolLibrary, toolLockFile, depsJsonPath);

            return depsJsonPath;
        }

        private string GetToolRuntimeConfigFilePath(string commandPath)
        {
            var commandName = Path.GetFileNameWithoutExtension(commandPath);
            var commandDirectory = Path.GetDirectoryName(commandPath);

            return Path.Combine(commandDirectory, commandName + FileNameSuffixes.RuntimeConfigJson);
        }

        private void CopyRuntimeConfigToToolHive(string toolHiveRoot, string runtimeConfigPath)
        {
            var destFile = Path.Combine(
                toolHiveRoot,
                Path.GetFileName(runtimeConfigPath));

            if (runtimeConfigPath == null || toolHiveRoot == null)
            {
                return;
            }

            if (!File.Exists(runtimeConfigPath))
            {
                Reporter.Verbose.WriteLine($"Runtime config doesn't exist for tool at {runtimeConfigPath}");
                return;
            }

            try
            {
                if (!File.Exists(destFile))
                {
                    File.Copy(runtimeConfigPath, toolHiveRoot);
                }
            }
            catch(Exception e)
            {
                Reporter.Error.WriteLine($"Failed to copy runtimeconfig from ${runtimeConfigPath} to ${toolHiveRoot}");
                throw e;
            }
        }

        private string GetCommandPath(
            LockFilePackageLibrary library, 
            string commandName, 
            IEnumerable<string> allowedExtensions,
            string nugetPackagesRoot)
        {
            var packageDirectory = new VersionFolderPathResolver(nugetPackagesRoot)
                .GetInstallPath(library.Name, library.Version);

            var commandRelativePath = library.Files
                    .Where(f => Path.GetFileNameWithoutExtension(f) == commandName)
                    .Where(e => allowedExtensions.Contains(Path.GetExtension(e)))
                    .FirstOrDefault();

            return Path.Combine(packageDirectory, commandRelativePath);
        }

        private void EnsureToolJsonDepsFileExists(
            LibraryRange toolLibrary, 
            LockFile toolLockFile, 
            string depsPath)
        {
            if (!File.Exists(depsPath))
            {
                var projectContext = new ProjectContextBuilder()
                    .WithLockFile(toolLockFile)
                    .WithTargetFramework(s_toolPackageFramework.ToString())
                    .Build();

                var exporter = projectContext.CreateExporter(Constants.DefaultConfiguration);

                var dependencyContext = new DependencyContextBuilder()
                    .Build(null, 
                        null, 
                        exporter.GetAllExports(), 
                        true, 
                        s_toolPackageFramework, 
                        string.Empty);

                using (var fileStream = File.Create(depsPath))
                {
                    var dependencyContextWriter = new DependencyContextWriter();

                    dependencyContextWriter.Write(dependencyContext, fileStream);
                }
            }
        }
    }
}
