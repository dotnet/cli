// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli.Compiler.Common;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.ProjectModel.Utilities;
using Microsoft.DotNet.Tools.Compiler;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.DotNet.ProjectModel.Compilation;
using NuGet.Protocol.Core.Types;

namespace Microsoft.DotNet.Tools.Build
{
    internal class IncrementalManager
    {
        private readonly ProjectBuilder _projectBuilder;
        private readonly CompilerIOManager _compilerIoManager;
        private readonly IncrementalPreconditionManager _preconditionManager;
        private readonly bool _shouldSkipDependencies;
        private readonly string _configuration;
        private readonly string _buildBasePath;
        private readonly string _outputPath;

        public IncrementalManager(
            ProjectBuilder projectBuilder,
            CompilerIOManager compilerIOManager,
            IncrementalPreconditionManager incrementalPreconditionManager,
            bool shouldSkipDependencies,
            string configuration,
            string buildBasePath,
            string outputPath)
        {
            _projectBuilder = projectBuilder;
            _compilerIoManager = compilerIOManager;
            _preconditionManager = incrementalPreconditionManager;
            _shouldSkipDependencies = shouldSkipDependencies;
            _configuration = configuration;
            _buildBasePath = buildBasePath;
            _outputPath = outputPath;
        }

        public IncrementalResult NeedsRebuilding(ProjectGraphNode graphNode)
        {
            if (!_shouldSkipDependencies &&
                graphNode.Dependencies.Any(d => _projectBuilder.GetCompilationResult(d) != CompilationResult.IncrementalSkip))
            {
                return new IncrementalResult("dependencies changed");
            }

            var preconditions = _preconditionManager.GetIncrementalPreconditions(graphNode);
            if (preconditions.PreconditionsDetected())
            {
                return new IncrementalResult($"project is not safe for incremental compilation. Use {BuildCommandApp.BuildProfileFlag} flag for more information.");
            }

            var compilerIO = _compilerIoManager.GetCompileIO(graphNode);

            var result = CLIChanged(graphNode);
            if (result.SkipBuild)
            {
                return result;
            }

            result = InputItemsChanged(graphNode, compilerIO);
            if (result.SkipBuild)
            {
                return result;
            }

            result = TimestampsChanged(compilerIO);
            if (result.SkipBuild)
            {
                return result;
            }

            return IncrementalResult.ShouldSkipBuild;
            
        }

        private IncrementalResult CLIChanged(ProjectGraphNode graphNode)
        {
            var currentVersionFile = DotnetFiles.VersionFile;
            var versionFileFromLastCompile = graphNode.ProjectContext.GetSDKVersionFile(_configuration, _buildBasePath, _outputPath);

            if (!File.Exists(currentVersionFile))
            {
                // this CLI does not have a version file; cannot tell if CLI changed
                return IncrementalResult.ShouldSkipBuild;
            }

            if (!File.Exists(versionFileFromLastCompile))
            {
                // this is the first compilation; cannot tell if CLI changed
                return IncrementalResult.ShouldSkipBuild;
            }

            var currentContent = DotnetFiles.ReadAndInterpretVersionFile();

            var versionsAreEqual = string.Equals(currentContent, File.ReadAllText(versionFileFromLastCompile), StringComparison.OrdinalIgnoreCase);

            return versionsAreEqual
                ? IncrementalResult.ShouldSkipBuild
                : new IncrementalResult("the version or bitness of the CLI changed since the last build");
        }

        private IncrementalResult InputItemsChanged(ProjectGraphNode graphNode, CompilerIO compilerIO)
        {
            // check empty inputs / outputs
            if (!compilerIO.Inputs.Any())
            {
                return new IncrementalResult("the project has no inputs");
            }

            if (!compilerIO.Outputs.Any())
            {
                return new IncrementalResult("the project has no outputs");
            }

            // check non existent items
            var result = CheckMissingIO(compilerIO.Inputs, "inputs");
            if (result.SkipBuild)
            {
                return result;
            }

            result = CheckMissingIO(compilerIO.Outputs, "outputs");
            if (result.SkipBuild)
            {
                return result;
            }

            // check cache against input glob pattern changes
            var incrementalCacheFile = graphNode.ProjectContext.IncrementalCacheFile(_configuration, _buildBasePath, _outputPath);

            if (!File.Exists(incrementalCacheFile))
            {
                // no cache present; cannot tell if anything changed
                return IncrementalResult.ShouldSkipBuild;
            }

            var incrementalCache = IncrementalCache.ReadFromFile(incrementalCacheFile);

            var diffResult = compilerIO.DiffInputs(incrementalCache.CompilerIO);

            if (diffResult.Deletions.Any())
            {
                return new IncrementalResult("Input items removed from last build", diffResult.Deletions);
            }

            if (diffResult.Additions.Any())
            {
                return new IncrementalResult("Input items added from last build", diffResult.Additions);
            }

            return IncrementalResult.ShouldSkipBuild;
        }

        private IncrementalResult CheckMissingIO(IEnumerable<string> items, string itemsType)
        {
            var missingItems = items.Where(i => !File.Exists(i)).ToList();

            return missingItems.Any()
                ? new IncrementalResult($"expected {itemsType} are missing", missingItems)
                : IncrementalResult.ShouldSkipBuild;
        }

        private IncrementalResult TimestampsChanged(CompilerIO compilerIO)
        {
            // find the output with the earliest write time
            var minDateUtc = File.GetLastWriteTimeUtc(compilerIO.Outputs.First());

            foreach (var outputPath in compilerIO.Outputs)
            {
                if (File.GetLastWriteTimeUtc(outputPath) >= minDateUtc)
                {
                    continue;
                }

                minDateUtc = File.GetLastWriteTimeUtc(outputPath);
            }

            // find inputs that are older than the earliest output
            var newInputs = compilerIO.Inputs.Where(p => File.GetLastWriteTimeUtc(p) >= minDateUtc);

            return newInputs.Any()
                ? new IncrementalResult("inputs were modified", newInputs)
                : IncrementalResult.ShouldSkipBuild;
        }

        public void CacheIncrementalState(ProjectGraphNode graphNode)
        {
            var incrementalCacheFile = graphNode.ProjectContext.IncrementalCacheFile(_configuration, _buildBasePath, _outputPath);
            IncrementalCache.WriteToFile(incrementalCacheFile, new IncrementalCache(_compilerIoManager.GetCompileIO(graphNode)));
        }
    }
}
