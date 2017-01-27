using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Tools.Common;
using NuGet.Frameworks;
using NuGet.ProjectModel;

namespace Microsoft.DotNet.Cli.Utils
{
    public class ProjectDependenciesCommandResolver : ICommandResolver
    {
        private const string ProjectDependenciesCommandResolverName = "projectdependenciescommandresolver";

        private static readonly CommandResolutionStrategy s_commandResolutionStrategy =
            CommandResolutionStrategy.ProjectDependenciesPackage;

        private readonly IEnvironmentProvider _environment;
        private readonly IPackagedCommandSpecFactory _packagedCommandSpecFactory;

        public ProjectDependenciesCommandResolver(
            IEnvironmentProvider environment,
            IPackagedCommandSpecFactory packagedCommandSpecFactory)
        {
            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            if (packagedCommandSpecFactory == null)
            {
                throw new ArgumentNullException(nameof(packagedCommandSpecFactory));
            }

            _environment = environment;
            _packagedCommandSpecFactory = packagedCommandSpecFactory;
        }

        public CommandSpec Resolve(CommandResolverArguments commandResolverArguments)
        {
            Reporter.Verbose.WriteLine(string.Format(
                LocalizableStrings.AttemptingToResolve,
                ProjectDependenciesCommandResolverName,
                commandResolverArguments.CommandName));

            if (commandResolverArguments.Framework == null
                || commandResolverArguments.ProjectDirectory == null
                || commandResolverArguments.Configuration == null
                || commandResolverArguments.CommandName == null)
            {
                Reporter.Verbose.WriteLine(string.Format(
                    LocalizableStrings.InvalidCommandResolverArguments,
                    ProjectDependenciesCommandResolverName));

                return null;
            }

            return ResolveFromProjectDependencies(
                    commandResolverArguments.ProjectDirectory,
                    commandResolverArguments.Framework,
                    commandResolverArguments.Configuration,
                    commandResolverArguments.CommandName,
                    commandResolverArguments.CommandArguments.OrEmptyIfNull(),
                    commandResolverArguments.OutputPath,
                    commandResolverArguments.BuildBasePath);
        }

        private CommandSpec ResolveFromProjectDependencies(
            string projectDirectory,
            NuGetFramework framework,
            string configuration,
            string commandName,
            IEnumerable<string> commandArguments,
            string outputPath,
            string buildBasePath)
        {
            var allowedExtensions = GetAllowedCommandExtensionsFromEnvironment(_environment);

            var projectFactory = new ProjectFactory(_environment);
            var project = projectFactory.GetProject(
                projectDirectory,
                framework,
                configuration,
                buildBasePath,
                outputPath);

            if (project == null)
            {
                Reporter.Verbose.WriteLine(string.Format(
                    LocalizableStrings.DidNotFindAMatchingProject,
                    ProjectDependenciesCommandResolverName,
                    projectDirectory));
                return null;
            }

            var depsFilePath = project.DepsJsonPath;

            if (!File.Exists(depsFilePath))
            {
                Reporter.Verbose.WriteLine(string.Format(
                    LocalizableStrings.DoesNotExist,
                    ProjectDependenciesCommandResolverName,
                    depsFilePath));
                return null;
            }

            var runtimeConfigPath = project.RuntimeConfigJsonPath;

            if (!File.Exists(runtimeConfigPath))
            {
                Reporter.Verbose.WriteLine(string.Format(
                    LocalizableStrings.DoesNotExist,
                    ProjectDependenciesCommandResolverName,
                    runtimeConfigPath));
                return null;
            }

            var lockFile = project.GetLockFile();
            var toolLibrary = GetToolLibraryForContext(lockFile, commandName, framework);
            var normalizedNugetPackagesRoot =
                PathUtility.EnsureNoTrailingDirectorySeparator(lockFile.PackageFolders.First().Path);

            var commandSpec = _packagedCommandSpecFactory.CreateCommandSpecFromLibrary(
                        toolLibrary,
                        commandName,
                        commandArguments,
                        allowedExtensions,
                        normalizedNugetPackagesRoot,
                        s_commandResolutionStrategy,
                        depsFilePath,
                        runtimeConfigPath);

            commandSpec?.AddEnvironmentVariablesFromProject(project);

            return commandSpec;
        }

        private LockFileTargetLibrary GetToolLibraryForContext(
            LockFile lockFile, string commandName, NuGetFramework targetFramework)
        {
            var toolLibraries = lockFile.Targets
                .FirstOrDefault(t => t.TargetFramework.GetShortFolderName()
                                      .Equals(targetFramework.GetShortFolderName()))
                ?.Libraries.Where(l => l.Name == commandName ||
                    l.RuntimeAssemblies.Any(r => Path.GetFileNameWithoutExtension(r.Path) == commandName)).ToList();

            if (toolLibraries?.Count() > 1)
            {
                throw new InvalidOperationException(string.Format(
                    LocalizableStrings.AmbiguousCommandName,
                    commandName));
            }

            Reporter.Verbose.WriteLine(string.Format(
                LocalizableStrings.ToolLibraryFound,
                ProjectDependenciesCommandResolverName,
                toolLibraries?.Count() > 0));

            return toolLibraries?.FirstOrDefault();
        }

        private IEnumerable<string> GetAllowedCommandExtensionsFromEnvironment(IEnvironmentProvider environment)
        {
            var allowedCommandExtensions = new List<string>();
            allowedCommandExtensions.AddRange(environment.ExecutableExtensions);
            allowedCommandExtensions.Add(FileNameSuffixes.DotNet.DynamicLib);

            return allowedCommandExtensions;
        }
    }
}
