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
    public class DepsJsonCommandResolver : ICommandResolver
    {
        private static readonly string[] s_extensionPreferenceOrder = new [] 
        { 
            "",
            "exe",
            "dll"
        };

        private Muxer _muxer;

        public DepsJsonCommandResolver()
        {
            _muxer = new Muxer();
        }

        public DepsJsonCommandResolver(Muxer muxer)
        {
            _muxer = muxer;
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

        public CommandSpec ResolveFromDepsJsonFile(
            string commandName, 
            IEnumerable<string> commandArgs, 
            string depsJsonFile)
        {
            var dependencyContext = LoadDependencyContextFromFile(depsJsonFile);

            var commandPath = FindCommandInDependencyContext(commandName, dependencyContext);
            if (commandPath == null)
            {
                Console.WriteLine("NULLLLLLL"); //todo remove
                return null;
            }

            return CreateCommandSpecUsingMuxer(commandPath, commandArgs, depsJsonFile);
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

        public string FindCommandInDependencyContext(string commandName, DependencyContext dependencyContext)
        {
            var commandCandidates = new List<string>();

            var assemblyCommandCandidates = GetAssemblyCommandCandidates(commandName, dependencyContext);
            var nativeCommandCandidates = GetNativeCommandCandidates(commandName, dependencyContext);
            
            commandCandidates.AddRange(assemblyCommandCandidates);
            commandCandidates.AddRange(nativeCommandCandidates);

            return ChooseCommandCandidate(commandCandidates);
        }

        public IEnumerable<string> GetAssemblyCommandCandidates(string commandName, DependencyContext dependencyContext)
        {
            return dependencyContext
                .RuntimeLibraries
                .SelectMany(r => r.Assemblies)
                .Select(a => a.Path)
                .Where(p => 
                    Path.GetFileNameWithoutExtension(p)
                    .Equals(commandName, StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<string> GetNativeCommandCandidates(string commandName, DependencyContext dependencyContext)
        {
            return dependencyContext
                .RuntimeLibraries
                .SelectMany(r => r.NativeLibraries)
                .Where(p => 
                    Path.GetFileNameWithoutExtension(p)
                    .Equals(commandName, StringComparison.OrdinalIgnoreCase));
        }

        public string ChooseCommandCandidate(IEnumerable<string> commandCandidates)
        {
            foreach (var extension in s_extensionPreferenceOrder)
            {
                var candidate = commandCandidates
                    .FirstOrDefault(p => Path.GetExtension(p).Equals(extension, StringComparison.OrdinalIgnoreCase));

                if (candidate != null)
                {
                    return candidate;
                }
            }

            return null;
        }

        public CommandSpec CreateCommandSpecUsingMuxer(
            string commandPath, 
            IEnumerable<string> commandArgs, 
            string depsJsonFile)
        {
            var depsFileArguments = GetDepsFileArgument(depsJsonFile);

            var muxerArgs = new List<string>();
            muxerArgs.Add(commandPath);
            muxerArgs.AddRange(depsFileArguments);
            muxerArgs.AddRange(commandArgs);

            var escapedArgString = ArgumentEscaper.EscapeAndConcatenateArgArrayForProcessStart(muxerArgs);

            return new CommandSpec(_muxer.MuxerPath, escapedArgString, CommandResolutionStrategy.DepsFile);
        }

        public IEnumerable<string> GetDepsFileArgument(string depsJsonFile)
        {
            return new[] { "--depsfile", depsJsonFile };
        }
    }
}
