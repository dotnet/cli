// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using NuGet.Frameworks;
using System.IO;
using System;
using Microsoft.DotNet.ProjectModel;

namespace Microsoft.DotNet.Cli.Utils
{
    public class DepsJsonCommandFactory : ICommandFactory
    {
        private DepsJsonCommandResolver _depsJsonCommandResolver;
        private string _temporaryDirectory;
        private string _depsJsonFile;
        private string _runtimeConfigFile;
        private string _nugetPackagesRoot;

        public DepsJsonCommandFactory(
            string depsJsonFile, 
            string runtimeConfigFile,
            string nugetPackagesRoot,
            string temporaryDirectory)
        {
            _depsJsonCommandResolver = new DepsJsonCommandResolver(nugetPackagesRoot);

            _temporaryDirectory = temporaryDirectory;
            _depsJsonFile = depsJsonFile;
            _runtimeConfigFile = runtimeConfigFile;
            _nugetPackagesRoot = nugetPackagesRoot;
        }

        public ICommand Create(
            string commandName,
            IEnumerable<string> args,
            NuGetFramework framework = null,
            string configuration = Constants.DefaultConfiguration)
        {
            var commandResolverArgs = new CommandResolverArguments()
            {
                CommandName = commandName,
                CommandArguments = args,
                DepsJsonFile = _depsJsonFile
            };

            var commandSpec = _depsJsonCommandResolver.Resolve(commandResolverArgs);

            var patchedCommandSpec = PatchCommandSpec(
                _temporaryDirectory, 
                commandName, 
                commandSpec,
                _runtimeConfigFile);

            return Command.Create(patchedCommandSpec);
        }

        private CommandSpec PatchCommandSpec(
            string temporaryDirectory, 
            string commandName, 
            CommandSpec commandSpec,
            string runtimeConfigFile)
        {
            var copiedCommand = CopyFileToTemp(temporaryDirectory, commandSpec.Path);
            var copiedRuntimeConfig = CopyFileToTemp(temporaryDirectory, runtimeConfigFile);
            var renamedRuntimeConfig = RenameRuntimeConfig(copiedRuntimeConfig, commandName);

            return new CommandSpec(
                copiedCommand,
                commandSpec.Args,
                commandSpec.ResolutionStrategy);
        }

        private string CopyFileToTemp(string temporaryDirectory, string sourceFile)
        {
            var destFile = Path.Combine(
                temporaryDirectory,
                Path.GetFileName(sourceFile));

            try
            {
                if (!File.Exists(destFile))
                {
                    File.Copy(sourceFile, temporaryDirectory);
                }
            }
            catch (Exception)
            {
                throw new Exception($"Unable to copy {sourceFile} to {destFile}");
            }

            return destFile;
        }

        private string RenameRuntimeConfig(string runtimeConfigFile, string commandName)
        {
            var newFileName = commandName + FileNameSuffixes.RuntimeConfigJson;

            var destDirectory = Path.GetDirectoryName(runtimeConfigFile);

            var destFile = Path.Combine(destDirectory, newFileName);

            if (!File.Exists(runtimeConfigFile))
            {
                throw new FileNotFoundException(runtimeConfigFile);
            }

            try
            {
                if (!File.Exists(destFile))
                {
                    File.Move(runtimeConfigFile, destFile);
                }
            }
            catch (Exception)
            {
                throw new Exception($"Unable to rename {runtimeConfigFile} to {destFile}");
            }

            return destFile;
        }
    }
}
