// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Tools.Compiler;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Cli.Compiler.Common;
using Microsoft.DotNet.ProjectModel.Utilities;
using NuGet.Frameworks;

namespace Microsoft.DotNet.Tools.Build
{
    internal class IncrementalPreconditionManager
    {
        private readonly bool _printPreconditions;
        private readonly bool _forceNonIncremental;
        private readonly bool _skipDependencies;
        private Dictionary<ProjectContextIdentity, IncrementalPreconditions> _preconditions;

        public IncrementalPreconditionManager(bool printPreconditions, bool forceNonIncremental, bool skipDependencies)
        {
            _printPreconditions = printPreconditions;
            _forceNonIncremental = forceNonIncremental;
            _skipDependencies = skipDependencies;
            _preconditions = new Dictionary<ProjectContextIdentity, IncrementalPreconditions>();
        }

        public static readonly string[] KnownCompilers = { "csc", "vbc", "fsc" };

        public IncrementalPreconditions GetIncrementalPreconditions(ProjectGraphNode projectNode)
        {
            IncrementalPreconditions preconditions;
            if (_preconditions.TryGetValue(projectNode.Identity, out preconditions))
            {
                return preconditions;
            }

            preconditions = new IncrementalPreconditions(_printPreconditions);

            if (_forceNonIncremental)
            {
                preconditions.AddForceUnsafePrecondition();
            }

            var project = projectNode.Project;
            CollectScriptPreconditions(project, preconditions);
            CollectCompilerNamePreconditions(project, preconditions);
            CollectCheckPathProbingPreconditions(project, projectNode.TargetFramework, preconditions);
            _preconditions[projectNode.Identity] = preconditions;
            return preconditions;
        }

        private void CollectCheckPathProbingPreconditions(Project project, NuGetFramework targetFramework, IncrementalPreconditions preconditions)
        {
            var pathCommands = CompilerUtil.GetCommandsInvokedByCompile(project)
                .Select(commandName => Command.CreateDotNet(commandName, Enumerable.Empty<string>(), targetFramework))
                .Where(c => c.ResolutionStrategy.Equals(CommandResolutionStrategy.Path));

            foreach (var pathCommand in pathCommands)
            {
                preconditions.AddPathProbingPrecondition(project.ProjectName(), pathCommand.CommandName);
            }
        }

        private void CollectCompilerNamePreconditions(Project project, IncrementalPreconditions preconditions)
        {
            if (project != null)
            {
                var projectCompiler = project.CompilerName;

                if (!KnownCompilers.Any(knownCompiler => knownCompiler.Equals(projectCompiler, StringComparison.Ordinal)))
                {
                    preconditions.AddUnknownCompilerPrecondition(project.ProjectName(), projectCompiler);
                }
            }
        }

        private void CollectScriptPreconditions(Project project, IncrementalPreconditions preconditions)
        {
            if (project != null)
            {
                var preCompileScripts = project.Scripts.GetOrEmpty(ScriptNames.PreCompile);
                var postCompileScripts = project.Scripts.GetOrEmpty(ScriptNames.PostCompile);

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