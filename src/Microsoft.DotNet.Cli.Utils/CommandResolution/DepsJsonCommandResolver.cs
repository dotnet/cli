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
            var dependencyContext = LoadDependencyContextFromFile(depsJsonFile);

            var commandPath = GetCommandPathFromDependencyContext(commandName, dependencyContext);
            if (commandPath == null)
            {
                return null;
            }

            return CreateCommandSpecUsingMuxerIfPortable(
                commandPath, 
                commandArgs, 
                depsJsonFile,
                CommandResolutionStrategy.DepsFile,
                _nugetPackageRoot,
                IsPortableApp(commandPath));
        }

        public DependencyContext LoadDependencyContextFromFile(string depsJsonFile)
        {
            DependencyContext dependencyContext = null;
            DependencyContextJsonReader contextReader = new DependencyContextJsonReader();

            using (var contextStream = File.OpenRead(depsJsonFile))
            {
                dependencyContext = contextReader.Read(contextStream);
            }

            return dependencyContext;
        }

        public string GetCommandPathFromDependencyContext(string commandName, DependencyContext dependencyContext)
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

        private CommandSpec CreateCommandSpecUsingMuxerIfPortable(
            string commandPath, 
            IEnumerable<string> commandArgs, 
            string depsJsonFile, 
            CommandResolutionStrategy commandResolutionStrategy,
            string nugetPackagesRoot,
            bool isPortable)
        {
            var depsFileArguments = GetDepsFileArguments(depsJsonFile);
            var additionalProbingPathArguments = GetAdditionalProbingPathArguments();

            var muxerArgs = new List<string>();
            muxerArgs.Add("exec");
            muxerArgs.AddRange(depsFileArguments);
            muxerArgs.AddRange(additionalProbingPathArguments);
            muxerArgs.Add(commandPath);
            muxerArgs.AddRange(commandArgs);

            var escapedArgString = ArgumentEscaper.EscapeAndConcatenateArgArrayForProcessStart(muxerArgs);

            return new CommandSpec(_muxer.MuxerPath, escapedArgString, commandResolutionStrategy);
        }

        // private CommandSpec CreateCommandSpecUsingMuxerIfPortable(
        //     string commandPath, 
        //     IEnumerable<string> commandArguments, 
        //     string depsFilePath,
        //     CommandResolutionStrategy commandResolutionStrategy,
        //     string nugetPackagesRoot,
        //     bool isPortable)
        // {
        //     var host = string.Empty;
        //     var arguments = new List<string>();

        //     if (isPortable)
        //     {
        //         var muxer = new Muxer();

        //         host = muxer.MuxerPath;
        //         if (host == null)
        //         {
        //             throw new Exception("Unable to locate dotnet multiplexer");
        //         }

        //         arguments.Add("exec");
        //     }
        //     else
        //     {
        //         host = CoreHost.HostExePath;
        //     }

        //     arguments.Add(commandPath);

        //     if (depsFilePath != null)
        //     {
        //         arguments.Add("--depsfile");
        //         arguments.Add(depsFilePath);
        //     }

        //     arguments.Add("--additionalprobingpath");
        //     arguments.Add(nugetPackagesRoot);

        //     arguments.AddRange(commandArguments);

        //     return CreateCommandSpec(host, arguments, commandResolutionStrategy);
        // }

        private bool IsPortableApp(string commandPath)
        {
            var commandDir = Path.GetDirectoryName(commandPath);

            var runtimeConfigPath = Directory.EnumerateFiles(commandDir)
                .FirstOrDefault(x => x.EndsWith("runtimeconfig.json"));

            if (runtimeConfigPath == null)
            {
                return false;
            }

            var runtimeConfig = new RuntimeConfig(runtimeConfigPath);

            return runtimeConfig.IsPortable;
        }

        private IEnumerable<string> GetDepsFileArguments(string depsJsonFile)
        {
            return new[] { "--depsfile", depsJsonFile };
        }

        private IEnumerable<string> GetAdditionalProbingPathArguments()
        {
            return new[] { "--additionalProbingPath", _nugetPackageRoot };
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
