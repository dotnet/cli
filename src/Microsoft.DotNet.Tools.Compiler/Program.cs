﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Dnx.Runtime.Common.CommandLine;
using Microsoft.DotNet.Cli.Compiler.Common;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.ProjectModel.Compilation;
using Microsoft.DotNet.ProjectModel.Graph;
using Microsoft.DotNet.ProjectModel.Utilities;
using NuGet.Frameworks;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.DotNet.Tools.Compiler
{
    public class Program
    {

        public static int Main(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);

            try
            {
                var compilerCommandArgs = new CompilerCommandApp("dotnet compile", ".NET Compiler", "Compiler for the .NET Platform");
                return compilerCommandArgs.Execute(OnExecute, args);
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.Error.WriteLine(ex);
#else
                Console.Error.WriteLine(ex.Message);
#endif
                return 1;
            }
        }

        private static bool OnExecute(List<ProjectContext> contexts, CompilerCommandApp args)
        {
            var success = true;

            foreach (var context in contexts)
            {
                success &= CompileProject(context, args);
                if (args.IsNativeValue && success)
                {
                    success &= CompileNative(context, args);
                }
            }
            return success;
        }

        private static bool CompileNative(
            ProjectContext context, 
            CompilerCommandApp args)
        {
            var outputPath = context.GetOutputPath(args.ConfigValue, args.OutputValue);
            var nativeOutputPath = Path.Combine(outputPath, "native");
            var intermediateOutputPath = 
                context.GetIntermediateOutputPath(args.ConfigValue, args.IntermediateValue, outputPath);
            var nativeIntermediateOutputPath = Path.Combine(intermediateOutputPath, "native");
            Directory.CreateDirectory(nativeOutputPath);
            Directory.CreateDirectory(nativeIntermediateOutputPath);

            var compilationOptions = context.ProjectFile.GetCompilerOptions(context.TargetFramework, args.ConfigValue);
            var managedOutput = 
                CompilerUtil.GetCompilationOutput(context.ProjectFile, context.TargetFramework, args.ConfigValue, outputPath);
            
            var nativeArgs = new List<string>();

            // Input Assembly
            nativeArgs.Add($"{managedOutput}");

            // ILC Args
            if (!string.IsNullOrWhiteSpace(args.IlcArgsValue))
            {
                nativeArgs.Add("--ilcargs");
                nativeArgs.Add($"{args.IlcArgsValue}");
            }            
            
            // ILC Path
            if (!string.IsNullOrWhiteSpace(args.IlcPathValue))
            {
                nativeArgs.Add("--ilcpath");
                nativeArgs.Add(args.IlcPathValue);
            }

            // ILC SDK Path
            if (!string.IsNullOrWhiteSpace(args.IlcSdkPathValue))
            {
                nativeArgs.Add("--ilcsdkpath");
                nativeArgs.Add(args.IlcSdkPathValue);
            }

            // AppDep SDK Path
            if (!string.IsNullOrWhiteSpace(args.AppDepSdkPathValue))
            {
                nativeArgs.Add("--appdepsdk");
                nativeArgs.Add(args.AppDepSdkPathValue);
            }

            // CodeGen Mode
            if(args.IsCppModeValue)
            {
                nativeArgs.Add("--mode");
                nativeArgs.Add("cpp");
            }

            if (!string.IsNullOrWhiteSpace(args.CppCompilerFlagsValue))
            {
                nativeArgs.Add("--cppcompilerflags");
                nativeArgs.Add(args.CppCompilerFlagsValue);
            }

            // Configuration
            if (args.ConfigValue != null)
            {
                nativeArgs.Add("--configuration");
                nativeArgs.Add(args.ConfigValue);
            }

            // Architecture
            if (args.ArchValue != null)
            {
                nativeArgs.Add("--arch");
                nativeArgs.Add(args.ArchValue);
            }

            // Intermediate Path
            nativeArgs.Add("--temp-output");
            nativeArgs.Add($"{nativeIntermediateOutputPath}");

            // Output Path
            nativeArgs.Add("--output");
            nativeArgs.Add($"{nativeOutputPath}");            

            // Write Response File
            var rsp = Path.Combine(nativeIntermediateOutputPath, $"dotnet-compile-native.{context.ProjectFile.Name}.rsp");
            File.WriteAllLines(rsp, nativeArgs);

            // TODO Add -r assembly.dll for all Nuget References
            //     Need CoreRT Framework published to nuget

            // Do Native Compilation
            var result = Command.Create("dotnet-compile-native", new string[] { "--rsp", $"{rsp}" })
                                .ForwardStdErr()
                                .ForwardStdOut()
                                .Execute();

            return result.ExitCode == 0;
        }

        private static bool CompileProject(ProjectContext context, CompilerCommandApp args)
        {
            // Set up Output Paths
            string outputPath = context.GetOutputPath(args.ConfigValue, args.OutputValue);
            string intermediateOutputPath = context.GetIntermediateOutputPath(args.ConfigValue, args.IntermediateValue, outputPath);

            Directory.CreateDirectory(outputPath);
            Directory.CreateDirectory(intermediateOutputPath);

            // Create the library exporter
            var exporter = context.CreateExporter(args.ConfigValue);

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
            var outputName = CompilerUtil.GetCompilationOutput(context.ProjectFile, context.TargetFramework, args.ConfigValue, outputPath);

            // Assemble args
            var compilerArgs = new List<string>()
            {
                $"--temp-output:\"{intermediateOutputPath}\"",
                $"--out:\"{outputName}\""
            };

            var compilationOptions = CompilerUtil.ResolveCompilationOptions(context, args.ConfigValue);

            var references = new List<string>();

            // Add compilation options to the args
            compilerArgs.AddRange(compilationOptions.SerializeToArgs());

            // Add metadata options
            compilerArgs.AddRange(AssemblyInfoOptions.SerializeToArgs(AssemblyInfoOptions.CreateForProject(context)));

            foreach (var dependency in dependencies)
            {
                var projectDependency = dependency.Library as ProjectDescription;

                if (projectDependency != null)
                {
                    if (projectDependency.Project.Files.SourceFiles.Any())
                    {
                        var projectOutputPath = CompilerUtil.GetCompilationOutput(projectDependency.Project, projectDependency.Framework, args.ConfigValue, outputPath);
                        references.Add(projectOutputPath);
                    }
                }
                else
                {
                    references.AddRange(dependency.CompilationAssemblies.Select(r => r.ResolvedPath));
                }

                compilerArgs.AddRange(dependency.SourceReferences.Select(s => $"\"{s}\""));
            }

            compilerArgs.AddRange(references.Select(r => $"--reference:\"{r}\""));

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

                compilerArgs.Add($"--resource:\"{depsJsonFile},{context.ProjectFile.Name}.deps.json\"");

                var refsFolder = Path.Combine(outputPath, "refs");
                if (Directory.Exists(refsFolder))
                {
                    Directory.Delete(refsFolder, true);
                }

                Directory.CreateDirectory(refsFolder);
                foreach (var reference in references)
                {
                    File.Copy(reference, Path.Combine(refsFolder, Path.GetFileName(reference)));
                }
            }

            if (!AddNonCultureResources(context.ProjectFile, compilerArgs, intermediateOutputPath))
            {
                return false;
            }
            // Add project source files
            var sourceFiles = CompilerUtil.GetCompilationSources(context);
            compilerArgs.AddRange(sourceFiles);

            var compilerName = CompilerUtil.ResolveCompilerName(context);

            // Write RSP file
            var rsp = Path.Combine(intermediateOutputPath, $"dotnet-compile.{context.ProjectFile.Name}.rsp");
            File.WriteAllLines(rsp, compilerArgs);

            // Run pre-compile event
            var contextVariables = new Dictionary<string, string>()
            {
                { "compile:TargetFramework", context.TargetFramework.DotNetFrameworkName },
                { "compile:Configuration", args.ConfigValue },
                { "compile:OutputFile", outputName },
                { "compile:OutputDir", outputPath },
                { "compile:ResponseFile", rsp }
            };
            RunScripts(context, ScriptNames.PreCompile, contextVariables);

            var result = Command.Create($"dotnet-compile-{compilerName}", new string[] {"@" + $"{rsp}" })
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
            RunScripts(context, ScriptNames.PostCompile, contextVariables);

            var success = result.ExitCode == 0;

            if (success)
            {
                success &= GenerateCultureResourceAssemblies(context.ProjectFile, dependencies, outputPath);
            }

            bool generateBindingRedirects = false;
            if (success && !args.NoHostValue && compilationOptions.EmitEntryPoint.GetValueOrDefault())
            {
                generateBindingRedirects = true;
                var rids = PlatformServices.Default.Runtime.GetAllCandidateRuntimeIdentifiers();
                var runtimeContext = ProjectContext.Create(context.ProjectDirectory, context.TargetFramework, rids);
                runtimeContext
                    .MakeCompilationOutputRunnable(outputPath, args.ConfigValue);
            }
            else if (!string.IsNullOrEmpty(context.ProjectFile.TestRunner))
            {
                generateBindingRedirects = true;
                var projectContext =
                    ProjectContext.Create(context.ProjectDirectory, context.TargetFramework,
                        new[] { PlatformServices.Default.Runtime.GetLegacyRestoreRuntimeIdentifier()});

                projectContext
                    .CreateExporter(args.ConfigValue)
                    .GetDependencies(LibraryType.Package)
                    .WriteDepsTo(Path.Combine(outputPath, projectContext.ProjectFile.Name + FileNameSuffixes.Deps));
            }

            if (generateBindingRedirects && context.TargetFramework.IsDesktop())
            {
                context.GenerateBindingRedirects(exporter, outputName);
            }

            return PrintSummary(diagnostics, sw, success);
        }



        private static void RunScripts(ProjectContext context, string name, Dictionary<string, string> contextVariables)
        {
            foreach (var script in context.ProjectFile.Scripts.GetOrEmpty(name))
            {
                ScriptExecutor.CreateCommandForScript(context.ProjectFile, script, contextVariables)
                    .ForwardStdErr()
                    .ForwardStdOut()
                    .Execute();
            }
        }
        
        private static void CopyExport(string outputPath, LibraryExport export)
        {
            CopyFiles(export.RuntimeAssemblies, outputPath);
            CopyFiles(export.NativeLibraries, outputPath);
        }

        private static bool PrintSummary(List<DiagnosticMessage> diagnostics, Stopwatch sw, bool success = true)
        {
            PrintDiagnostics(diagnostics);

            Reporter.Output.WriteLine();

            var errorCount = diagnostics.Count(d => d.Severity == DiagnosticMessageSeverity.Error);
            var warningCount = diagnostics.Count(d => d.Severity == DiagnosticMessageSeverity.Warning);

            if (errorCount > 0 || !success)
            {
                Reporter.Output.WriteLine("Compilation failed.".Red());
                success = false;
            }
            else
            {
                Reporter.Output.WriteLine("Compilation succeeded.".Green());
            }

            Reporter.Output.WriteLine($"    {warningCount} Warning(s)");
            Reporter.Output.WriteLine($"    {errorCount} Error(s)");

            Reporter.Output.WriteLine();

            Reporter.Output.WriteLine($"Time elapsed {sw.Elapsed}");

            return success;
        }

        private static bool AddNonCultureResources(Project project, List<string> compilerArgs, string intermediateOutputPath)
        {
            var resgenFiles = CompilerUtil.GetNonCultureResources(project, intermediateOutputPath);

            foreach (var resgenFile in resgenFiles)
            {
                if (ResourceUtility.IsResxFile(resgenFile.InputFile))
                {
                    var result =
                        Command.Create("dotnet-resgen",
                            new string[]
                            {
                                $"{resgenFile.InputFile}",
                                "-o",
                                $"{resgenFile.OutputFile}",
                                "-v",
                                $"{project.Version.Version}"
                            })
                            .ForwardStdErr()
                            .ForwardStdOut()
                            .Execute();

                    if (result.ExitCode != 0)
                    {
                        return false;
                    }

                    compilerArgs.Add($"--resource:\"{resgenFile.OutputFile},{Path.GetFileName(resgenFile.MetadataName)}\"");
                }
                else
                {
                    compilerArgs.Add($"--resource:\"{resgenFile.InputFile},{Path.GetFileName(resgenFile.MetadataName)}\"");
                }
            }

            return true;
        }

        private static bool GenerateCultureResourceAssemblies(Project project, List<LibraryExport> dependencies, string outputPath)
        {
            var referencePaths = CompilerUtil.GetReferencePathsForCultureResgen(dependencies);
            var resgenReferenceArgs = referencePaths.Select(referencePath => $"-r \"{referencePath}\"").ToList();

            var cultureResgenFiles = CompilerUtil.GetCultureResources(project, outputPath);

            foreach (var resgenFile in cultureResgenFiles)
            {
                var resourceOutputPath = Path.GetDirectoryName(resgenFile.OutputFile);

                if (!Directory.Exists(resourceOutputPath))
                {
                    Directory.CreateDirectory(resourceOutputPath);
                }

                var arguments = new List<string>();

                arguments.AddRange(resgenReferenceArgs);
                arguments.Add($"-o \"{resgenFile.OutputFile}\"");
                arguments.Add($"-c {resgenFile.Culture}");
                arguments.Add($"-v {project.Version.Version}");
                arguments.AddRange(resgenFile.InputFileToMetadata.Select(fileToMetadata => $"\"{fileToMetadata.Key}\",{fileToMetadata.Value}"));

                var result = Command.Create("dotnet-resgen", arguments)
                                        .ForwardStdErr()
                                        .ForwardStdOut()
                                        .Execute();
                if (result.ExitCode != 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static DiagnosticMessage ParseDiagnostic(string projectRootPath, string line)
        {
            var error = CanonicalError.Parse(line);

            if (error != null)
            {
                var severity = error.category == CanonicalError.Parts.Category.Error ?
                DiagnosticMessageSeverity.Error : DiagnosticMessageSeverity.Warning;

                return new DiagnosticMessage(
                    error.code,
                    error.text,
                    Path.IsPathRooted(error.origin) ? line : projectRootPath + Path.DirectorySeparatorChar + line,
                    Path.Combine(projectRootPath, error.origin),
                    severity,
                    error.line,
                    error.column,
                    error.endColumn,
                    error.endLine,
                    source: null);
            }

            return null;
        }

        private static void PrintDiagnostics(List<DiagnosticMessage> diagnostics)
        {
            foreach (var diag in diagnostics)
            {
                PrintDiagnostic(diag);
            }
        }

        private static void PrintDiagnostic(DiagnosticMessage diag)
        {
            switch (diag.Severity)
            {
                case DiagnosticMessageSeverity.Info:
                    Reporter.Error.WriteLine(diag.FormattedMessage);
                    break;
                case DiagnosticMessageSeverity.Warning:
                    Reporter.Error.WriteLine(diag.FormattedMessage.Yellow().Bold());
                    break;
                case DiagnosticMessageSeverity.Error:
                    Reporter.Error.WriteLine(diag.FormattedMessage.Red().Bold());
                    break;
            }
        }

        private static void CopyFiles(IEnumerable<LibraryAsset> files, string outputPath)
        {
            foreach (var file in files)
            {
                File.Copy(file.ResolvedPath, Path.Combine(outputPath, Path.GetFileName(file.ResolvedPath)), overwrite: true);
            }
        }

        private static string EnsureTrailingSlash(string path)
        {
            return EnsureTrailingCharacter(path, Path.DirectorySeparatorChar);
        }

        private static string EnsureTrailingCharacter(string path, char trailingCharacter)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            // if the path is empty, we want to return the original string instead of a single trailing character.
            if (path.Length == 0 || path[path.Length - 1] == trailingCharacter)
            {
                return path;
            }

            return path + trailingCharacter;
        }
    }
}
