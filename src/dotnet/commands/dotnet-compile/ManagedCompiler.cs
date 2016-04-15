// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.Compiler.Common;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.ProjectModel.Compilation;
using Microsoft.Extensions.DependencyModel;
using NuGet.Frameworks;

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

            var compilationOptions = context.ResolveCompilationOptions(args.ConfigValue);

            // Set default platform if it isn't already set and we're on desktop
            if (compilationOptions.EmitEntryPoint == true && string.IsNullOrEmpty(compilationOptions.Platform) && context.TargetFramework.IsDesktop())
            {
                // See https://github.com/dotnet/cli/issues/2428 for more details.
                compilationOptions.Platform = RuntimeInformation.ProcessArchitecture == Architecture.X64 ?
                    "x64" : "anycpu32bitpreferred";
            }

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
                    var resourceName = ResourceManifestName.CreateManifestName(
                        Path.GetFileName(resourceFile.ResolvedPath), compilationOptions.OutputName);
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
                var allExports = exporter.GetAllExports().ToList();
                var dependencyContext = new DependencyContextBuilder().Build(compilationOptions,
                    allExports,
                    allExports,
                    false, // For now, just assume non-portable mode in the legacy deps file (this is going away soon anyway)
                    context.TargetFramework,
                    context.RuntimeIdentifier ?? string.Empty);

                var writer = new DependencyContextWriter();
                var depsJsonFile = Path.Combine(intermediateOutputPath, compilationOptions.OutputName + "dotnet-compile.deps.json");
                using (var fileStream = File.Create(depsJsonFile))
                {
                    writer.Write(dependencyContext, fileStream);
                }

                compilerArgs.Add($"--resource:\"{depsJsonFile}\",{compilationOptions.OutputName}.deps.json");
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

                contextVariables.Add(
                    "compile:RuntimeIdentifier",
                    runtimeContext.RuntimeIdentifier);
            }

            _scriptRunner.RunScripts(context, ScriptNames.PreCompile, contextVariables);

            string commandName = $"compile-{compilerName}";
            string commandArg = $"@{rsp}";

            CommandResult result;
            Func<string[], int> builtInCommand;
            if (Program.TryGetBuiltInCommand(commandName, out builtInCommand))
            {
                TextWriter originalConsoleOut = Console.Out;
                TextWriter originalConsoleError = Console.Error;
                try
                {
                    // redirect the standard out and error so we can parse the diagnostics
                    var outputWriter = new LineNotificationTextWriter(originalConsoleOut.FormatProvider, originalConsoleOut.Encoding)
                        .OnWriteLine(line => HandleCompilerOutputLine(line, context, diagnostics, Reporter.Output));
                    Console.SetOut(outputWriter);

                    var errorWriter = new LineNotificationTextWriter(originalConsoleError.FormatProvider, originalConsoleError.Encoding)
                        .OnWriteLine(line => HandleCompilerOutputLine(line, context, diagnostics, Reporter.Error));
                    Console.SetError(errorWriter);

                    int exitCode = builtInCommand(new[] { commandArg });

                    // fake out a ProcessStartInfo for the reporting code below
                    ProcessStartInfo startInfo = new ProcessStartInfo(new Muxer().MuxerPath, $"{commandName} {commandArg}");
                    result = new CommandResult(startInfo, exitCode, null, null);
                }
                finally
                {
                    Console.SetOut(originalConsoleOut);
                    Console.SetError(originalConsoleError);
                }
            }
            else
            {
                result = _commandFactory.Create(commandName, new[] { commandArg })
                    .OnErrorLine(line => HandleCompilerOutputLine(line, context, diagnostics, Reporter.Error))
                    .OnOutputLine(line => HandleCompilerOutputLine(line, context, diagnostics, Reporter.Output))
                    .Execute();
            }

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

        private static void HandleCompilerOutputLine(string line, ProjectContext context, List<DiagnosticMessage> diagnostics, Reporter reporter)
        {
            var diagnostic = ParseDiagnostic(context.ProjectDirectory, line);
            if (diagnostic != null)
            {
                diagnostics.Add(diagnostic);
            }
            else
            {
                reporter.WriteLine(line);
            }
        }

        /// <summary>
        /// A TextWriter that can raises an event for each line that is written to it.
        /// </summary>
        private class LineNotificationTextWriter : TextWriter
        {
            private Encoding _encoding;
            private StringBuilder _currentString;
            private Action<string> _lineHandler;

            public LineNotificationTextWriter(IFormatProvider formatProvider, Encoding encoding)
                : base(formatProvider)
            {
                _encoding = encoding;

                // start with an average line length so the builder doesn't need to immediately grow
                _currentString = new StringBuilder(128);
            }

            public LineNotificationTextWriter OnWriteLine(Action<string> lineHandler)
            {
                Debug.Assert(lineHandler != null);
                Debug.Assert(_lineHandler == null);

                _lineHandler = lineHandler;

                return this;
            }

            public override Encoding Encoding
            {
                get { return _encoding; }
            }

            public override void Write(char value)
            {
                string currentLine = null;

                lock (_currentString)
                {
                    if (value == '\n')
                    {
                        currentLine = _currentString.ToString();
                        _currentString.Clear();
                    }
                    else
                    {
                        _currentString.Append(value);
                    }
                }

                if (currentLine != null)
                {
                    // invoke the handler outside of the lock
                    _lineHandler?.Invoke(currentLine);
                }
            }
        }
    }
}
