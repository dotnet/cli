// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Tools.Compiler;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Cli.Compiler.Common;
using Microsoft.DotNet.ProjectModel.Utilities;

namespace Microsoft.DotNet.Tools.Build
{
    internal class IncrementalPreconditionManager
    {
        private readonly bool _printPreconditions;
        private readonly bool _forceNonIncremental;
        private readonly bool _skipDependencies;
        private readonly string _configuration;
        private Dictionary<ProjectContextIdentity, IncrementalPreconditions> _preconditions;

        public IncrementalPreconditionManager(bool printPreconditions, bool forceNonIncremental, bool skipDependencies, string configuration)
        {
            _printPreconditions = printPreconditions;
            _forceNonIncremental = forceNonIncremental;
            _skipDependencies = skipDependencies;
            _configuration = configuration;
            _preconditions = new Dictionary<ProjectContextIdentity, IncrementalPreconditions>();
        }

        public static readonly string[] KnownCompilers = { "csc", "vbc", "fsc" };

        public IncrementalPreconditions GetIncrementalPreconditions(ProjectGraphNode projectNode)
        {
            IncrementalPreconditions preconditions;
            if (_preconditions.TryGetValue(projectNode.ProjectContext.Identity, out preconditions))
            {
                return preconditions;
            }

            preconditions = new IncrementalPreconditions(_printPreconditions);

            if (_forceNonIncremental)
            {
                preconditions.AddForceUnsafePrecondition();
            }

            var project = projectNode.ProjectContext;
            CollectScriptPreconditions(project, preconditions);
            CollectCompilerNamePreconditions(project, preconditions);
            CollectCheckPathProbingPreconditions(project, preconditions);
            _preconditions[projectNode.ProjectContext.Identity] = preconditions;
            return preconditions;
        }

        private void CollectCheckPathProbingPreconditions(ProjectContext project, IncrementalPreconditions preconditions)
        {

            var compilerOptions = project.ProjectFile.GetCompilerOptions(project.TargetFramework, configurationName: null);
            var compilerName =  compilerOptions.CompilerName;

            HandleCompilerRunner(project, preconditions, compilerName);
        }

        private void HandleCompilerRunner(ProjectContext project, IncrementalPreconditions preconditions, string compilerName)
        {
            // mimic the resolution in ManagedCompiler
            var commandFromManagedCompiler = new DotNetCommandFactory().Create($"compile-{compilerName}",
                Enumerable.Empty<string>());

            if (commandFromManagedCompiler.ResolutionStrategy.Equals(CommandResolutionStrategy.BuiltIn))
            {
                return;
            }
            
            // if it's not a builtin, managed compiler delegates to the muxer which delegates back to the dotnet driver
            // mimic the resolution in the dotnet driver
            var commandFromDotnetDriver = Command.Create($"dotnet-compile-{compilerName}", Enumerable.Empty<string>());

            if (LoadedFromPath(commandFromDotnetDriver))
            {
                preconditions.AddPathProbingPrecondition(project.ProjectName(), commandFromDotnetDriver);
            }
        }

        private static bool LoadedFromPath(ICommand c)
        {
            var resolutionStrategy = c.ResolutionStrategy;

            return
                resolutionStrategy.Equals(CommandResolutionStrategy.Path) ||
                resolutionStrategy.Equals(CommandResolutionStrategy.RootedPath) ||
                resolutionStrategy.Equals(CommandResolutionStrategy.ProjectLocal) ||
                resolutionStrategy.Equals(CommandResolutionStrategy.OutputPath);
        }

        private void CollectCompilerNamePreconditions(ProjectContext project, IncrementalPreconditions preconditions)
        {
            if (project.ProjectFile != null)
            {
                var compilerOptions = project.ProjectFile.GetCompilerOptions(project.TargetFramework, null);
                var projectCompiler = compilerOptions.CompilerName;

                if (!KnownCompilers.Any(knownCompiler => knownCompiler.Equals(projectCompiler, StringComparison.Ordinal)))
                {
                    preconditions.AddUnknownCompilerPrecondition(project.ProjectName(), projectCompiler);
                }
            }
        }

        private void CollectScriptPreconditions(ProjectContext project, IncrementalPreconditions preconditions)
        {
            if (project.ProjectFile != null)
            {
                var preCompileScripts = project.ProjectFile.Scripts.GetOrEmpty(ScriptNames.PreCompile);
                var postCompileScripts = project.ProjectFile.Scripts.GetOrEmpty(ScriptNames.PostCompile);

                if (preCompileScripts.Any())
                {
                    preconditions.AddPrePostScriptPrecondition(project.ProjectName(), ScriptNames.PreCompile);
                }

                if (postCompileScripts.Any())
                {
                    preconditions.AddPrePostScriptPrecondition(project.ProjectName(), ScriptNames.PostCompile);
                }
            }
        }

    }
}