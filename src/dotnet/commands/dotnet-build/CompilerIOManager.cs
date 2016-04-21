// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Tools.Compiler;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Cli.Compiler.Common;

namespace Microsoft.DotNet.Tools.Build
{
    internal class CompilerIOManager
    {
        private readonly string _configuration;
        private readonly string _outputPath;
        private readonly string _buildBasePath;
        private readonly IList<string> _runtimes;
        private readonly WorkspaceContext _workspace;
		private readonly ConcurrentDictionary<ProjectContextIdentity, CompilerIO> _cache;

        public CompilerIOManager(string configuration,
            string outputPath,
            string buildBasePath,
            IEnumerable<string> runtimes,
            WorkspaceContext workspace)
        {
            _configuration = configuration;
            _outputPath = outputPath;
            _buildBasePath = buildBasePath;
            _runtimes = runtimes.ToList();
            _workspace = workspace;
            
            _cache = new ConcurrentDictionary<ProjectContextIdentity, CompilerIO>();
        }


        // computes all the inputs and outputs that would be used in the compilation of a project
        public CompilerIO GetCompileIO(ProjectGraphNode graphNode)
        {
            return _cache.GetOrAdd(graphNode.ProjectContext.Identity, i => ComputeIO(graphNode));
        }

        public CompilerIO ComputeIO(ProjectGraphNode graphNode, IEnumerable<ProjectContext> runtimeContexts)
        {
            var inputs = new List<string>();
            var outputs = new List<string>();

            var isRootProject = graphNode.IsRoot;
            var project = graphNode.ProjectContext;

            var calculator = project.GetOutputPaths(_configuration, _buildBasePath, _outputPath);
            var binariesOutputPath = calculator.CompilationOutputPath;

            // input: project.json
            inputs.Add(project.ProjectFile.ProjectFilePath);

            // input: lock file; find when dependencies change
            AddLockFile(project, inputs);

            // input: source files
            inputs.AddRange(CompilerUtil.GetCompilationSources(project));

            var allOutputPath = new HashSet<string>(calculator.CompilationFiles.All());
            foreach (var runtimeContext in runtimeContexts)
            {
                foreach (var path in runtimeContext.GetOutputPaths(_configuration, _buildBasePath, _outputPath).RuntimeFiles.All())
                {
                    allOutputPath.Add(path);
                }
            }

            // output: compiler outputs
            foreach (var path in allOutputPath)
            {
                outputs.Add(path);
            }

            // input compilation options files
            AddCompilationOptions(project, _configuration, inputs);

            // input / output: resources with culture
            AddNonCultureResources(project, calculator.IntermediateOutputDirectoryPath, inputs, outputs);

            // input / output: resources without culture
            AddCultureResources(project, binariesOutputPath, inputs, outputs);

            return new CompilerIO(inputs, outputs);
        }

        private static void AddLockFile(ProjectContext project, List<string> inputs)
        {
            if (project.LockFile == null)
            {
                var errorMessage = $"Project {project.ProjectName()} does not have a lock file.";
                Reporter.Error.WriteLine(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            inputs.Add(project.LockFile.LockFilePath);

            if (project.LockFile.ExportFile != null)
            {
                inputs.Add(project.LockFile.ExportFile.ExportFilePath);
            }
        }


        private static void AddCompilationOptions(ProjectContext project, string config, List<string> inputs)
        {
            var compilerOptions = project.ResolveCompilationOptions(config);

            // input: key file
            if (compilerOptions.KeyFile != null)
            {
                inputs.Add(compilerOptions.KeyFile);
            }
        }

        private static void AddNonCultureResources(ProjectContext project, string intermediaryOutputPath, List<string> inputs, IList<string> outputs)
        {
            foreach (var resourceIO in CompilerUtil.GetNonCultureResources(project.ProjectFile, intermediaryOutputPath))
            {
                inputs.Add(resourceIO.InputFile);

                if (resourceIO.OutputFile != null)
                {
                    outputs.Add(resourceIO.OutputFile);
                }
            }
        }

        private static void AddCultureResources(ProjectContext project, string outputPath, List<string> inputs, List<string> outputs)
        {
            foreach (var cultureResourceIO in CompilerUtil.GetCultureResources(project.ProjectFile, outputPath))
            {
                inputs.AddRange(cultureResourceIO.InputFileToMetadata.Keys);

                if (cultureResourceIO.OutputFile != null)
                {
                    outputs.Add(cultureResourceIO.OutputFile);
                }
            }
        }
    }
}