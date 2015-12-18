// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.Dnx.Runtime.Common.CommandLine;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.Tools.Compiler;
using NuGet.Frameworks;



namespace Microsoft.DotNet.Tools.Build
{
    public class Program
    {
        public static int Main(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);

            try
            {
                var app = new CompilerCommandApp("dotnet build", ".NET Builder", "Builder for the .NET Platform. It performs incremental compilation if it's safe to do so. Otherwise it delegates to dotnet-compile which performs non-incremental compilation");
                return app.Execute(OnExecute, args);
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
            foreach (var context in contexts)
            {
                // Set up Output Paths
                string outputPath = context.GetOutputPath(args.ConfigValue, args.OutputValue);
                string intermediateOutputPath = context.GetIntermediateOutputPath(args.ConfigValue, args.IntermediateValue, outputPath);

                Directory.CreateDirectory(outputPath);
                Directory.CreateDirectory(intermediateOutputPath);

                //compile dependencies
                if (!CompileDependencies(context, outputPath, intermediateOutputPath, args))
                {
                    return false;
                }
                
                //compile project
                if (!CompileProject(context, outputPath, intermediateOutputPath, args))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool CompileDependencies(ProjectContext context, string outputPath, string intermediateOutputPath, CompilerCommandApp args)
        {
            // Create the library exporter
            var exporter = context.CreateExporter(args.ConfigValue);

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
                if (!CompileDependency(projectDependency, outputPath, intermediateOutputPath, args))
                    return false;
            }

            projects.Clear()
;
            return true;
        }

        private static bool CompileDependency(ProjectDescription projectDependency, string outputPath, string intermediateOutputPath, CompilerCommandApp args)
        {
            var compileResult = Command.Create("dotnet-compile",
                $"--framework {projectDependency.Framework} " +
                $"--configuration {args.ConfigValue} " +
                $"--output \"{outputPath}\" " +
                $"--temp-output \"{intermediateOutputPath}\" " +
                (args.NoHostValue ? "--no-host " : string.Empty) +
                $"\"{projectDependency.Project.ProjectDirectory}\"")
                .ForwardStdOut()
                .ForwardStdErr()
                .Execute();

            return compileResult.ExitCode == 0;
        }

        private static bool CompileProject(ProjectContext context, string outputPath, string intermediateOutputPath, CompilerCommandApp args)
        {
            var compileResult = Command.Create("dotnet-compile",
                $"--framework {context.TargetFramework} " +
                $"--configuration {args.ConfigValue} " +
                $"--output \"{outputPath}\" " +
                $"--temp-output \"{intermediateOutputPath}\" " +
                (args.NoHostValue ? "--no-host " : string.Empty) +
                //nativeArgs
                (args.IsNativeValue ? "--native " : string.Empty) +
                (args.IsCppModeValue ? "--cpp " : string.Empty) +
                (!string.IsNullOrWhiteSpace(args.ArchValue) ? $"--arch {args.ArchValue} " : string.Empty) +
                (!string.IsNullOrWhiteSpace(args.IlcArgsValue) ? $"--ilcargs \"{args.IlcArgsValue}\" " : string.Empty) +
                (!string.IsNullOrWhiteSpace(args.IlcPathValue) ? $"--ilcpath \"{args.IlcPathValue}\" " : string.Empty) +
                (!string.IsNullOrWhiteSpace(args.IlcSdkPathValue) ? $"--ilcsdkpath \"{args.IlcSdkPathValue}\" " : string.Empty) +
                (!string.IsNullOrWhiteSpace(args.AppDepSdkPathValue) ? $"--appdepsdkpath \"{args.AppDepSdkPathValue}\" " : string.Empty) +
                $"\"{context.ProjectDirectory}\"")
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
