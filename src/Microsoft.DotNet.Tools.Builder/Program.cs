// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.Dnx.Runtime.Common.CommandLine;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ProjectModel;
using NuGet.Frameworks;


namespace Microsoft.DotNet.Tools.Build
{
    public class Program
    {
        public static int Main(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);

            var app = new CommandLineApplication();
            app.Name = "dotnet build";
            app.FullName = ".NET Builder";
            app.Description = "Builder for the .NET Platform. It performs incremental compilation if it's safe to do so. Otherwise it delegates to dotnet-compile which performs non-incremental compilation";
            app.HelpOption("-h|--help");

            var output = app.Option("-o|--output <OUTPUT_DIR>", "Directory in which to place outputs", CommandOptionType.SingleValue);
            var intermediateOutput = app.Option("-t|--temp-output <OUTPUT_DIR>", "Directory in which to place temporary outputs", CommandOptionType.SingleValue);
            var framework = app.Option("-f|--framework <FRAMEWORK>", "Compile a specific framework", CommandOptionType.MultipleValue);
            var configuration = app.Option("-c|--configuration <CONFIGURATION>", "Configuration under which to build", CommandOptionType.SingleValue);
            var noHost = app.Option("--no-host", "Set this to skip publishing a runtime host when building for CoreCLR", CommandOptionType.NoValue);
            var project = app.Argument("<PROJECT>", "The project to compile, defaults to the current directory. Can be a path to a project.json or a project directory");

            // Native Args
            var native = app.Option("-n|--native", "Compiles source to native machine code.", CommandOptionType.NoValue);
            var arch = app.Option("-a|--arch <ARCH>", "The architecture for which to compile. x64 only currently supported.", CommandOptionType.SingleValue);
            var ilcArgs = app.Option("--ilcargs <ARGS>", "Command line arguments to be passed directly to ILCompiler.", CommandOptionType.SingleValue);
            var ilcPath = app.Option("--ilcpath <PATH>", "Path to the folder containing custom built ILCompiler.", CommandOptionType.SingleValue);
            var ilcSdkPath = app.Option("--ilcsdkpath <PATH>", "Path to the folder containing ILCompiler application dependencies.", CommandOptionType.SingleValue);
            var cppMode = app.Option("--cpp", "Flag to do native compilation with C++ code generator.", CommandOptionType.NoValue);

            app.OnExecute(() =>
            {
                // Locate the project and get the name and full path
                var path = project.Value;
                if (string.IsNullOrEmpty(path))
                {
                    path = Directory.GetCurrentDirectory();
                }

                var isNative = native.HasValue();
                var isCppMode = cppMode.HasValue();
                var archValue = arch.Value();
                var ilcArgsValue = ilcArgs.Value();
                var ilcPathValue = ilcPath.Value();
                var ilcSdkPathValue = ilcSdkPath.Value();
                var configValue = configuration.Value() ?? Constants.DefaultConfiguration;
                var outputValue = output.Value();
                var intermediateValue = intermediateOutput.Value();
                var isNoHost = noHost.HasValue();

                var contexts = framework.HasValue() ?
                    framework.Values.Select(f => ProjectContext.Create(path, NuGetFramework.Parse(f))) :
                    ProjectContext.CreateContextForEachFramework(path);

                bool success = true;

                foreach (var context in contexts)
                {
                    // Set up Output Paths
                    string outputPath = context.GetOutputPath(configValue, outputValue);
                    string intermediateOutputPath = context.GetIntermediateOutputPath(configValue, intermediateValue, outputValue);

                    Directory.CreateDirectory(outputPath);
                    Directory.CreateDirectory(intermediateOutputPath);

                    //compile dependencies
                    success &= CompileDependencies(context, configValue, outputPath, intermediateOutputPath, isNoHost);

                    if (!success)
                    {
                        return 1;
                    }

                    //compile project
                    success &= CompileProject(context, configValue, outputPath, intermediateOutputPath, isNoHost, isNative, isCppMode, archValue, ilcArgsValue, ilcPathValue, ilcSdkPathValue, path);
                }

                return success ? 0 : 1;
            });

            try
            {
                return app.Execute(args);
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

        private static bool CompileDependencies(ProjectContext context, string configuration, string outputPath, string intermediateOutputPath, bool noHost)
        {
            // Create the library exporter
            var exporter = context.CreateExporter(configuration);

            // Gather exports for the project
            var dependencies = exporter.GetDependencies().ToList();

            var projects = new Dictionary<string, ProjectDescription>();

            // Build project references
            foreach (var dependency in dependencies)
            {
                var projectDependency = dependency.Library as ProjectDescription;

                if (projectDependency != null && projectDependency.Project.Files.SourceFiles.Any())
                {
                    projects[projectDependency.Identity.Name] = projectDependency;
                }
            }

            foreach (var projectDependency in Sort(projects))
            {
                if (!CompileDependency(projectDependency, configuration, noHost, outputPath, intermediateOutputPath))
                    return false;
            }

            projects.Clear()
;
            return true;
        }

        private static bool CompileDependency(ProjectDescription projectDependency, string configuration, bool noHost, string outputPath, string intermediateOutputPath)
        {
            var compileResult = Command.Create("dotnet-compile",
                $"--framework {projectDependency.Framework} " +
                $"--configuration {configuration} " +
                $"--output \"{outputPath}\" " +
                $"--temp-output \"{intermediateOutputPath}\" " +
                (noHost ? "--no-host " : string.Empty) +
                $"\"{projectDependency.Project.ProjectDirectory}\"")
                .ForwardStdOut()
                .ForwardStdErr()
                .Execute();

            return compileResult.ExitCode == 0;
        }

        private static bool CompileProject(ProjectContext context, string configValue, string outputPath,
            string intermediateOutputPath, bool isNoHost, bool isNative, bool isCppMode, string archValue, string ilcArgsValue,
            string ilcPathValue, string ilcSdkPathValue, string path)
        {
            var compileResult = Command.Create("dotnet-compile",
                $"--framework {context.TargetFramework} " +
                $"--configuration {configValue} " +
                $"--output \"{outputPath}\" " +
                $"--temp-output \"{intermediateOutputPath}\" " +
                (isNoHost ? "--no-host " : string.Empty) +
                //nativeArgs
                (isNative ? "--native " : string.Empty) +
                (isCppMode ? "--cpp " : string.Empty) +
                (!string.IsNullOrWhiteSpace(archValue) ? $"--arch {archValue} " : string.Empty) +
                (!string.IsNullOrWhiteSpace(ilcArgsValue) ? $"--ilcargs \"{ilcArgsValue}\" " : string.Empty) +
                (!string.IsNullOrWhiteSpace(ilcPathValue) ? $"--ilcpath \"{ilcPathValue}\" " : string.Empty) +
                (!string.IsNullOrWhiteSpace(ilcSdkPathValue) ? $"--ilcsdkpath \"{ilcSdkPathValue}\" " : string.Empty) +
                $"\"{path}\"")
                .ForwardStdOut()
                .ForwardStdErr()
                .Execute();

            return compileResult.ExitCode == 0;
        }

        private static ISet<ProjectDescription> Sort(Dictionary<string, ProjectDescription> projects)
        {
            var outputs = new HashSet<ProjectDescription>();

            foreach (var pair in projects)
            {
                Sort(pair.Value, projects, outputs);
            }

            return outputs;
        }

        private static void Sort(ProjectDescription project, Dictionary<string, ProjectDescription> projects, ISet<ProjectDescription> outputs)
        {
            // Sorts projects in dependency order so that we only build them once per chain
            foreach (var dependency in project.Dependencies)
            {
                ProjectDescription projectDependency;
                if (projects.TryGetValue(dependency.Name, out projectDependency))
                {
                    Sort(projectDependency, projects, outputs);
                }
            }

            outputs.Add(project);
        }
    }
}
