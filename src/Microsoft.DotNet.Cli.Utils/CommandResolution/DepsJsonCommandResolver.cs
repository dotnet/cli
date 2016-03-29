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
using Microsoft.DotNet.ProjectModel.Resolution;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.ProjectModel;

using LockFile = Microsoft.DotNet.ProjectModel.Graph.LockFile;
using FileFormatException = Microsoft.DotNet.ProjectModel.FileFormatException;

namespace Microsoft.DotNet.Cli.Utils
{
    public class DepsJsonCommandResolver : ICommandResolver
    {
        private static readonly string[] s_extensionPreferenceOrder = new [] 
        { 
            "",
            ".exe",
            ".dll"
        };

        private string _nugetPackageRoot;
        private Muxer _muxer;

        public DepsJsonCommandResolver()
        {
            _muxer = new Muxer();
            _nugetPackageRoot = PackageDependencyProvider.ResolvePackagesPath(null, null);
        }

        public DepsJsonCommandResolver(string nugetPackageRoot)
        {
            _muxer = new Muxer();
            _nugetPackageRoot = nugetPackageRoot;
        }

        public DepsJsonCommandResolver(Muxer muxer, string nugetPackageRoot)
        {
            _muxer = muxer;
            _nugetPackageRoot = nugetPackageRoot;
        }

        public CommandSpec Resolve(CommandResolverArguments commandResolverArguments)
        {
            if (commandResolverArguments.CommandName == null
                || commandResolverArguments.DepsJsonFile == null)
            {
                return null;
            }
            
            return ResolveFromDepsJsonFile(
                commandResolverArguments.CommandName, 
                commandResolverArguments.CommandArguments.OrEmptyIfNull(),
                commandResolverArguments.DepsJsonFile);
        }

        private CommandSpec ResolveFromDepsJsonFile(
            string commandName, 
            IEnumerable<string> commandArgs, 
            string depsJsonFile)
        {
            Console.WriteLine("depsJson");
            Console.WriteLine(depsJsonFile);
            var dependencyContext = LoadDependencyContextFromFile(depsJsonFile);

            var commandPath = FindCommandInDependencyContext(commandName, dependencyContext);
            if (commandPath == null)
            {
                Console.WriteLine("NULLLLLLL"); //todo remove
                return null;
            }

            return CreateCommandSpecUsingMuxer(commandPath, commandArgs, depsJsonFile);
        }

        private DependencyContext LoadDependencyContextFromFile(string depsJsonFile)
        {
            DependencyContext dependencyContext = null;
            DependencyContextJsonReader contextReader = new DependencyContextJsonReader();

            using (var contextStream = File.OpenRead(depsJsonFile))
            {
                dependencyContext = contextReader.Read(contextStream);
            }

            return dependencyContext;
        }

        private string FindCommandInDependencyContext(string commandName, DependencyContext dependencyContext)
        {
            var commandCandidates = new List<CommandCandidate>();

            var assemblyCommandCandidates = GetAssemblyCommandCandidates(commandName, dependencyContext);
            var nativeCommandCandidates = GetNativeCommandCandidates(commandName, dependencyContext);

            commandCandidates.AddRange(assemblyCommandCandidates);
            commandCandidates.AddRange(nativeCommandCandidates);

            var command = ChooseCommandCandidate(commandCandidates);

            return command?.GetAbsoluteCommandPath(_nugetPackageRoot);
        }

        private IEnumerable<CommandCandidate> GetAssemblyCommandCandidates(string commandName, DependencyContext dependencyContext)
        {
            var commandCandidates = new List<CommandCandidate>();

            foreach (var runtimeLibrary in dependencyContext.RuntimeLibraries)
            {
                var runtimeAssetGroups = runtimeLibrary.RuntimeAssemblyGroups;

                commandCandidates.AddRange(GetCommandCandidatesFromRuntimeAssetGroups(
                    commandName, 
                    runtimeAssetGroups,
                    runtimeLibrary.Name,
                    runtimeLibrary.Version));
            }

            return commandCandidates;
        }

        private IEnumerable<CommandCandidate> GetNativeCommandCandidates(string commandName, DependencyContext dependencyContext)
        {
            var commandCandidates = new List<CommandCandidate>();

            foreach (var runtimeLibrary in dependencyContext.RuntimeLibraries)
            {
                var runtimeAssetGroups = runtimeLibrary.NativeLibraryGroups;

                commandCandidates.AddRange(GetCommandCandidatesFromRuntimeAssetGroups(
                                    commandName,
                                    runtimeAssetGroups,
                                    runtimeLibrary.Name,
                                    runtimeLibrary.Version));            
            }

            return commandCandidates;
        }

        private IEnumerable<CommandCandidate> GetCommandCandidatesFromRuntimeAssetGroups(
            string commandName,
            IEnumerable<RuntimeAssetGroup> runtimeAssetGroups, 
            string PackageName,
            string PackageVersion)
        {
            var candidateAssetGroups = runtimeAssetGroups
                .Where(r => r.Runtime == string.Empty)
                .Where(a => 
                    a.AssetPaths.Any(p => Path.GetFileNameWithoutExtension(p)
                    .Equals(commandName, StringComparison.OrdinalIgnoreCase)));

            var commandCandidates = new List<CommandCandidate>();
            foreach (var candidateAssetGroup in candidateAssetGroups)
            {
                var candidateAssetPaths = candidateAssetGroup.AssetPaths.Where(
                    p => Path.GetFileNameWithoutExtension(p)
                    .Equals(commandName, StringComparison.OrdinalIgnoreCase));

                foreach (var candidateAssetPath in candidateAssetPaths)
                {
                    commandCandidates.Add(new CommandCandidate
                    {
                        PackageName = PackageName,
                        PackageVersion = PackageVersion,
                        RelativeCommandPath = candidateAssetPath
                    });
                }
            }

            return commandCandidates;
        }

        private CommandCandidate ChooseCommandCandidate(IEnumerable<CommandCandidate> commandCandidates)
        {
            foreach (var extension in s_extensionPreferenceOrder)
            {
                var candidate = commandCandidates
                    .FirstOrDefault(p => Path.GetExtension(p.RelativeCommandPath)
                        .Equals(extension, StringComparison.OrdinalIgnoreCase));

                if (candidate != null)
                {
                    return candidate;
                }
            }

            return null;
        }

        private CommandSpec CreateCommandSpecUsingMuxer(
            string commandPath, 
            IEnumerable<string> commandArgs, 
            string depsJsonFile)
        {
            var depsFileArguments = GetDepsFileArgument(depsJsonFile);

            var muxerArgs = new List<string>();
            muxerArgs.Add("exec");
            muxerArgs.Add(commandPath);
            muxerArgs.AddRange(depsFileArguments);
            muxerArgs.AddRange(commandArgs);

            var escapedArgString = ArgumentEscaper.EscapeAndConcatenateArgArrayForProcessStart(muxerArgs);

            return new CommandSpec(_muxer.MuxerPath, escapedArgString, CommandResolutionStrategy.DepsFile);
        }

        private IEnumerable<string> GetDepsFileArgument(string depsJsonFile)
        {
            return new[] { "--depsfile", depsJsonFile };
        }

        private class CommandCandidate
        {
            public string PackageName { get; set; }
            public string PackageVersion { get; set; }
            public string RelativeCommandPath { get; set; }

            public string GetAbsoluteCommandPath(string nugetPackageRoot)
            {
                return Path.Combine(nugetPackageRoot, PackageName, PackageVersion, RelativeCommandPath);
            }
        }
    }
}
