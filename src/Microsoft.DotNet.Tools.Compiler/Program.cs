﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Dnx.Runtime.Common.CommandLine;
using Microsoft.Dotnet.Cli.Compiler.Common;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Cli.Compiler.Common;
using Microsoft.DotNet.Tools.Common;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.ProjectModel.Compilation;
using Microsoft.DotNet.ProjectModel.Utilities;
using NuGet.Frameworks;
using Microsoft.Extensions.DependencyModel;

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

            Directory.CreateDirectory(nativeOutputPath);
            Directory.CreateDirectory(intermediateOutputPath);

            var compilationOptions = context.ProjectFile.GetCompilerOptions(context.TargetFramework, args.ConfigValue);
            var managedOutput = 
                GetProjectOutput(context.ProjectFile, context.TargetFramework, args.ConfigValue, outputPath);
            
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
            nativeArgs.Add($"{intermediateOutputPath}");

            // Output Path
            nativeArgs.Add("--output");
            nativeArgs.Add($"{nativeOutputPath}");            

            // Write Response File
            var rsp = Path.Combine(intermediateOutputPath, $"dotnet-compile-native.{context.ProjectFile.Name}.rsp");
            File.WriteAllLines(rsp, nativeArgs);

            // TODO Add -r assembly.dll for all Nuget References
            //     Need CoreRT Framework published to nuget

            // Do Native Compilation
            var result = Command.Create("dotnet-compile-native", $"--rsp \"{rsp}\"")
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
            var outputName = GetProjectOutput(context.ProjectFile, context.TargetFramework, args.ConfigValue, outputPath);

            // Assemble args
            var compilerArgs = new List<string>()
            {
                $"--temp-output:{intermediateOutputPath}",
                $"--out:{outputName}"
            };

            var compilationOptions = context.ProjectFile.GetCompilerOptions(context.TargetFramework, args.ConfigValue);

            // Path to strong naming key in environment variable overrides path in project.json
            var environmentKeyFile = Environment.GetEnvironmentVariable(EnvironmentNames.StrongNameKeyFile);

            if (!string.IsNullOrWhiteSpace(environmentKeyFile))
            {
                compilationOptions.KeyFile = environmentKeyFile;
            }
            else  if (!string.IsNullOrWhiteSpace(compilationOptions.KeyFile))
            {
                // Resolve full path to key file
                compilationOptions.KeyFile = Path.GetFullPath(Path.Combine(context.ProjectFile.ProjectDirectory, compilationOptions.KeyFile));
            }

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
                        var projectOutputPath = GetProjectOutput(projectDependency.Project, projectDependency.Framework, args.ConfigValue, outputPath);
                        references.Add(projectOutputPath);
                    }
                }
                else
                {
                    references.AddRange(dependency.CompilationAssemblies.Select(r => r.ResolvedPath));
                }

                compilerArgs.AddRange(dependency.SourceReferences);
            }

            compilerArgs.AddRange(references.Select(r => $"--reference:{r}"));

            var runtimeContext = ProjectContext.Create(context.ProjectDirectory, context.TargetFramework, new[] { RuntimeIdentifier.Current });
            var libraryExporter = runtimeContext.CreateExporter(args.ConfigValue);

            if (compilationOptions.PreserveCompilationContext == true)
            {
                var dependencyContext = DependencyContextBuilder.FromLibraryExporter(
                    libraryExporter, context.TargetFramework.DotNetFrameworkName, context.RuntimeIdentifier);

                var writer = new DependencyContextWriter();
                var depsJsonFile = Path.Combine(intermediateOutputPath, context.ProjectFile.Name + "dotnet-compile.deps.json");
                using (var fileStream = File.Create(depsJsonFile))
                {
                    writer.Write(dependencyContext, fileStream);
                }

                compilerArgs.Add($"--resource:\"{depsJsonFile}\",{context.ProjectFile.Name}.deps.json");

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

            if (!AddResources(context.ProjectFile, compilerArgs, intermediateOutputPath))
            {
                return false;
            }
            // Add project source files
            var sourceFiles = context.ProjectFile.Files.SourceFiles;
            compilerArgs.AddRange(sourceFiles);

            var compilerName = context.ProjectFile.CompilerName;
            compilerName = compilerName ?? "csc";

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

            var result = Command.Create($"dotnet-compile-{compilerName}", $"@\"{rsp}\"")
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
                success &= GenerateResourceAssemblies(context.ProjectFile, dependencies, outputPath, args.ConfigValue);
            }

            if (success && !args.NoHostValue && compilationOptions.EmitEntryPoint.GetValueOrDefault())
            {
                MakeRunnable(runtimeContext,
                             outputPath,
                             libraryExporter);
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

        private static string GetProjectOutput(Project project, NuGetFramework framework, string configuration, string outputPath)
        {
            var compilationOptions = project.GetCompilerOptions(framework, configuration);
            var outputExtension = ".dll";

            if (framework.IsDesktop() && compilationOptions.EmitEntryPoint.GetValueOrDefault())
            {
                outputExtension = ".exe";
            }

            return Path.Combine(outputPath, project.Name + outputExtension);
        }

        private static void CleanOrCreateDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                try
                {
                    Directory.Delete(path, recursive: true);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unable to remove directory: " + path);
                    Console.WriteLine(e.Message);
                }
            }

            Directory.CreateDirectory(path);
        }

        private static void MakeRunnable(ProjectContext runtimeContext, string outputPath, LibraryExporter exporter)
        {
            CopyContents(runtimeContext, outputPath);

            if (runtimeContext.TargetFramework.IsDesktop())
            {
                // On desktop we need to copy dependencies since we don't own the host
                foreach (var export in exporter.GetDependencies())
                {
                    CopyExport(outputPath, export);
                }

                GenerateBindingRedirects(runtimeContext, outputPath, exporter);
            }
            else
            {
                EmitHost(runtimeContext, outputPath, exporter);
            }
        }

        private static void GenerateBindingRedirects(ProjectContext runtimeContext, string outputPath, LibraryExporter exporter)
        {
            var appConfigNames = new[] { "app.config", "App.config" };
            XDocument baseAppConfig = null;

            foreach (var appConfigName in appConfigNames)
            {
                var baseAppConfigPath = Path.Combine(runtimeContext.ProjectDirectory, appConfigName);

                if (File.Exists(baseAppConfigPath))
                {
                    using (var fileStream = File.OpenRead(baseAppConfigPath))
                    {
                        baseAppConfig = XDocument.Load(fileStream);
                        break;
                    }
                }
            }

            var generator = new BindingRedirectGenerator();
            var appConfig = generator.Generate(exporter.GetAllExports(), baseAppConfig);

            if (appConfig != null)
            {
                var path = Path.Combine(outputPath, runtimeContext.ProjectFile.Name + ".exe.config");
                using (var stream = File.Create(path))
                {
                    appConfig.Save(stream);
                }
            }
        }

        private static void CopyExport(string outputPath, LibraryExport export)
        {
            CopyFiles(export.RuntimeAssemblies, outputPath);
            CopyFiles(export.NativeLibraries, outputPath);
        }

        private static void EmitHost(ProjectContext runtimeContext, string outputPath, LibraryExporter exporter)
        {
            // Write the Host information file (basically a simplified form of the lock file)
            var lines = new List<string>();
            foreach (var export in exporter.GetAllExports())
            {
                if (export.Library == runtimeContext.RootProject)
                {
                    continue;
                }

                if (export.Library is ProjectDescription)
                {
                    // Copy project dependencies to the output folder
                    CopyFiles(export.RuntimeAssemblies, outputPath);
                    CopyFiles(export.NativeLibraries, outputPath);
                }
                else
                {
                    lines.AddRange(GenerateLines(export, export.RuntimeAssemblies, "runtime"));
                    lines.AddRange(GenerateLines(export, export.NativeLibraries, "native"));
                }
            }

            File.WriteAllLines(Path.Combine(outputPath, runtimeContext.ProjectFile.Name + ".deps"), lines);

            // Copy the host in
            CopyHost(Path.Combine(outputPath, runtimeContext.ProjectFile.Name + Constants.ExeSuffix));
        }

        private static void CopyHost(string target)
        {
            var hostPath = Path.Combine(AppContext.BaseDirectory, Constants.HostExecutableName);
            File.Copy(hostPath, target, overwrite: true);
        }

        private static IEnumerable<string> GenerateLines(LibraryExport export, IEnumerable<LibraryAsset> items, string type)
        {
            return items.Select(item =>
                EscapeCsv(export.Library.Identity.Type.Value) + "," +
                EscapeCsv(export.Library.Identity.Name) + "," +
                EscapeCsv(export.Library.Identity.Version.ToNormalizedString()) + "," +
                EscapeCsv(export.Library.Hash) + "," +
                EscapeCsv(type) + "," +
                EscapeCsv(item.Name) + "," +
                EscapeCsv(item.RelativePath) + ",");
        }

        private static string EscapeCsv(string input)
        {
            return "\"" + input.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
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
            Reporter.Output.WriteLine();

            return success;
        }

        private static bool AddResources(Project project, List<string> compilerArgs, string intermediateOutputPath)
        {

            foreach (var resourceFile in project.Files.ResourceFiles)
            {
                var resourcePath = resourceFile.Key;

                if (!string.IsNullOrEmpty(ResourceUtility.GetResourceCultureName(resourcePath)))
                {
                    // Include only "neutral" resources into main assembly
                    continue;
                }

                var name = GetResourceFileMetadataName(project, resourceFile);

                var fileName = resourcePath;

                if (ResourceUtility.IsResxFile(fileName))
                {
                    // {file}.resx -> {file}.resources
                    var resourcesFile = Path.Combine(intermediateOutputPath, name);

                    var result = Command.Create("dotnet-resgen", $"\"{fileName}\" -o \"{resourcesFile}\" -v \"{project.Version.Version}\"")
                                        .ForwardStdErr()
                                        .ForwardStdOut()
                                        .Execute();

                    if (result.ExitCode != 0)
                    {
                        return false;
                    }

                    // Use this as the resource name instead
                    fileName = resourcesFile;
                }

                compilerArgs.Add($"--resource:\"{fileName}\",{name}");
            }

            return true;
        }

        private static string GetResourceFileMetadataName(Project project, KeyValuePair<string, string> resourceFile)
        {
            string resourceName = null;
            string rootNamespace = null;

            string root = PathUtility.EnsureTrailingSlash(project.ProjectDirectory);
            string resourcePath = resourceFile.Key;
            if (string.IsNullOrEmpty(resourceFile.Value))
            {
                // No logical name, so use the file name
                resourceName = ResourceUtility.GetResourceName(root, resourcePath);
                rootNamespace = project.Name;
            }
            else
            {
                resourceName = ResourceManifestName.EnsureResourceExtension(resourceFile.Value, resourcePath);
                rootNamespace = null;
            }

            var name = ResourceManifestName.CreateManifestName(resourceName, rootNamespace);
            return name;
        }

        private static bool GenerateResourceAssemblies(Project project, List<LibraryExport> dependencies, string outputPath, string configuration)
        {
            var references = dependencies.SelectMany(libraryExport => libraryExport.CompilationAssemblies);

            foreach (var resourceFileGroup in project.Files.ResourceFiles.GroupBy(file => ResourceUtility.GetResourceCultureName(file.Key)))
            {
                var culture = resourceFileGroup.Key;
                if (!string.IsNullOrEmpty(culture))
                {
                    var resourceOutputPath = Path.Combine(outputPath, culture);

                    if (!Directory.Exists(resourceOutputPath))
                    {
                        Directory.CreateDirectory(resourceOutputPath);
                    }

                    var resourceOuputFile = Path.Combine(resourceOutputPath, project.Name + ".resources.dll");

                    var arguments = new List<string>();
                    arguments.AddRange(references.Select(r => $"-r \"{r.ResolvedPath}\""));
                    arguments.Add($"-o \"{resourceOuputFile}\"");
                    arguments.Add($"-c {culture}");
                    arguments.Add($"-v {project.Version.Version}");

                    foreach (var resourceFile in resourceFileGroup)
                    {
                        var metadataName = GetResourceFileMetadataName(project, resourceFile);
                        arguments.Add($"\"{resourceFile.Key}\",{metadataName}");
                    }

                    var result = Command.Create("dotnet-resgen", arguments)
                                            .ForwardStdErr()
                                            .ForwardStdOut()
                                            .Execute();
                    if (result.ExitCode != 0)
                    {
                        return false;
                    }
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

        private static void CopyContents(ProjectContext context, string outputPath)
        {
            var sourceFiles = context.ProjectFile.Files.GetCopyToOutputFiles();
            Copy(sourceFiles, context.ProjectDirectory, outputPath);
        }

        private static void Copy(IEnumerable<string> sourceFiles, string sourceDirectory, string targetDirectory)
        {
            if (sourceFiles == null)
            {
                throw new ArgumentNullException(nameof(sourceFiles));
            }

            sourceDirectory = EnsureTrailingSlash(sourceDirectory);
            targetDirectory = EnsureTrailingSlash(targetDirectory);

            foreach (var sourceFilePath in sourceFiles)
            {
                var fileName = Path.GetFileName(sourceFilePath);

                var targetFilePath = sourceFilePath.Replace(sourceDirectory, targetDirectory);
                var targetFileParentFolder = Path.GetDirectoryName(targetFilePath);

                // Create directory before copying a file
                if (!Directory.Exists(targetFileParentFolder))
                {
                    Directory.CreateDirectory(targetFileParentFolder);
                }

                File.Copy(
                    sourceFilePath,
                    targetFilePath,
                    overwrite: true);

                // clear read-only bit if set
                var fileAttributes = File.GetAttributes(targetFilePath);
                if ((fileAttributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    File.SetAttributes(targetFilePath, fileAttributes & ~FileAttributes.ReadOnly);
                }
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
