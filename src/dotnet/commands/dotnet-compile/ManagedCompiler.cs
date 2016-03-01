﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli.Compiler.Common;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.ProjectModel.Compilation;
using Microsoft.DotNet.ProjectModel.Utilities;
using Microsoft.Extensions.DependencyModel;

namespace Microsoft.DotNet.Tools.Compiler
{
    public class ManagedCompiler : Compiler
    {
        private readonly IScriptRunner _scriptRunner;
        private readonly ICommandFactory _commandFactory;

        public ManagedCompiler(IScriptRunner scriptRunner, ICommandFactory commandFactory)
        {
            _scriptRunner = scriptRunner;
            _commandFactory = commandFactory;
        }

        public override bool Compile(ProjectContext context, CompilerCommandApp args)
        {
            // Set up Output Paths
            var outputPaths = context.GetOutputPaths(args.ConfigValue, args.BuildBasePathValue);
            var outputPath = outputPaths.CompilationOutputPath;
            var intermediateOutputPath = outputPaths.IntermediateOutputDirectoryPath;

            Directory.CreateDirectory(outputPath);
            Directory.CreateDirectory(intermediateOutputPath);

            // Create the library exporter
            var exporter = context.CreateExporter(args.ConfigValue, args.BuildBasePathValue);

            // Gather exports for the project
            var dependencies = exporter.GetDependencies().ToList();

            Reporter.Output.WriteLine($"Compiling {context.RootProject.Identity.Name.Yellow()} for {context.TargetFramework.DotNetFrameworkName.Yellow()}");
            var sw = Stopwatch.StartNew();

            var diagnostics = new List<DiagnosticMessage>();
            var missingFrameworkDiagnostics = new List<DiagnosticMessage>();

            // Collect dependency diagnostics
            foreach (var diag in context.LibraryManager.GetAllDiagnostics())
            {
                if (diag.ErrorCode == ErrorCodes.DOTNET1011 ||
                    diag.ErrorCode == ErrorCodes.DOTNET1012)
                {
                    missingFrameworkDiagnostics.Add(diag);
                }

                diagnostics.Add(diag);
            }

            if (missingFrameworkDiagnostics.Count > 0)
            {
                // The framework isn't installed so we should short circuit the rest of the compilation
                // so we don't get flooded with errors
                PrintSummary(missingFrameworkDiagnostics, sw);
                return false;
            }

            // Get compilation options
            var outputName = outputPaths.CompilationFiles.Assembly;

            // Assemble args
            var compilerArgs = new List<string>()
            {
                $"--temp-output:{intermediateOutputPath}",
                $"--out:{outputName}"
            };

            var compilationOptions = CompilerUtil.ResolveCompilationOptions(context, args.ConfigValue);
            var languageId = CompilerUtil.ResolveLanguageId(context);

            var references = new List<string>();

            // Add compilation options to the args
            compilerArgs.AddRange(compilationOptions.SerializeToArgs());

            // Add metadata options
            compilerArgs.AddRange(AssemblyInfoOptions.SerializeToArgs(AssemblyInfoOptions.CreateForProject(context)));

            foreach (var dependency in dependencies)
            {
                references.AddRange(dependency.CompilationAssemblies.Select(r => r.ResolvedPath));

                compilerArgs.AddRange(dependency.SourceReferences.Select(s => s.GetTransformedFile(intermediateOutputPath)));

                foreach (var resourceFile in dependency.EmbeddedResources)
                {
                    var transformedResource = resourceFile.GetTransformedFile(intermediateOutputPath);
                    var resourceName = ResourceManifestName.CreateManifestName(Path.GetFileName(resourceFile.ResolvedPath), context.ProjectFile.Name);
                    compilerArgs.Add($"--resource:\"{transformedResource}\",{resourceName}");
                }

                // Add analyzer references
                compilerArgs.AddRange(dependency.AnalyzerReferences
                    .Where(a => a.AnalyzerLanguage == languageId)
                    .Select(a => $"--analyzer:{a.AssemblyPath}"));
            }

            compilerArgs.AddRange(references.Select(r => $"--reference:{r}"));

            if (compilationOptions.PreserveCompilationContext == true)
            {
                var dependencyContext = DependencyContextBuilder.Build(compilationOptions,
                    exporter,
                    args.ConfigValue,
                    context.TargetFramework,
                    context.RuntimeIdentifier);

                var writer = new DependencyContextWriter();
                var depsJsonFile = Path.Combine(intermediateOutputPath, context.ProjectFile.Name + "dotnet-compile.deps.json");
                using (var fileStream = File.Create(depsJsonFile))
                {
                    writer.Write(dependencyContext, fileStream);
                }

                compilerArgs.Add($"--resource:\"{depsJsonFile}\",{context.ProjectFile.Name}.deps.json");
            }

            if (!AddNonCultureResources(context.ProjectFile, compilerArgs, intermediateOutputPath))
            {
                return false;
            }
            // Add project source files
            var sourceFiles = CompilerUtil.GetCompilationSources(context);
            compilerArgs.AddRange(sourceFiles);

            var compilerName = context.ProjectFile.CompilerName;

            // Write RSP file
            var rsp = Path.Combine(intermediateOutputPath, $"dotnet-compile.rsp");
            File.WriteAllLines(rsp, compilerArgs);

            // Run pre-compile event
            var contextVariables = new Dictionary<string, string>()
            {
                { "compile:TargetFramework", context.TargetFramework.GetShortFolderName() },
                { "compile:FullTargetFramework", context.TargetFramework.DotNetFrameworkName },
                { "compile:Configuration", args.ConfigValue },
                { "compile:OutputFile", outputName },
                { "compile:OutputDir", outputPath.TrimEnd('\\', '/') },
                { "compile:ResponseFile", rsp }
            };

            if (context.ProjectFile.HasRuntimeOutput(args.ConfigValue))
            {
                var runtimeContext = context.CreateRuntimeContext(args.GetRuntimes());
                var runtimeOutputPath = runtimeContext.GetOutputPaths(args.ConfigValue, args.BuildBasePathValue, args.OutputValue);

                contextVariables.Add(
                    "compile:RuntimeOutputDir",
                    runtimeOutputPath.RuntimeOutputPath.TrimEnd('\\', '/'));
            }

            _scriptRunner.RunScripts(context, ScriptNames.PreCompile, contextVariables);

            var result = _commandFactory.Create($"compile-{compilerName}", new[] { "@" + $"{rsp}" })
                .OnErrorLine(line =>
                {
                    var diagnostic = ParseDiagnostic(context.ProjectDirectory, line);
                    if (diagnostic != null)
                    {
                        diagnostics.Add(diagnostic);
                    }
                    else
                    {
                        Reporter.Error.WriteLine(line);
                    }
                })
                .OnOutputLine(line =>
                {
                    var diagnostic = ParseDiagnostic(context.ProjectDirectory, line);
                    if (diagnostic != null)
                    {
                        diagnostics.Add(diagnostic);
                    }
                    else
                    {
                        Reporter.Output.WriteLine(line);
                    }
                }).Execute();

            // Run post-compile event
            contextVariables["compile:CompilerExitCode"] = result.ExitCode.ToString();
            _scriptRunner.RunScripts(context, ScriptNames.PostCompile, contextVariables);

            var success = result.ExitCode == 0;

            if (!success)
            {
                Reporter.Error.WriteLine($"{result.StartInfo.FileName} {result.StartInfo.Arguments} returned Exit Code {result.ExitCode}");
            }

            if (success)
            {
                success &= GenerateCultureResourceAssemblies(context.ProjectFile, dependencies, intermediateOutputPath, outputPath);
            }

            return PrintSummary(diagnostics, sw, success);
        }
    }
}
