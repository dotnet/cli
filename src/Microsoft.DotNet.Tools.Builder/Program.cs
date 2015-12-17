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

        private static bool OnExecute(List<ProjectContext> contexts, string configValue, string outputValue, string intermediateValue, bool noHost,
            bool isNative, string archValue, string ilcArgsValue, string ilcPathValue, string ilcSdkPathValue,
            bool isCppMode)
        {
            foreach (var context in contexts)
            {
                // Set up Output Paths
                string outputPath = context.GetOutputPath(configValue, outputValue);
                string intermediateOutputPath = context.GetIntermediateOutputPath(configValue, intermediateValue, outputValue);

                Directory.CreateDirectory(outputPath);
                Directory.CreateDirectory(intermediateOutputPath);

                //compile dependencies
                if (!CompileDependencies(context, configValue, outputPath, intermediateOutputPath, noHost))
                {
                    return false;
                }
                
                //compile project
                if (!CompileProject(context, configValue, outputPath, intermediateOutputPath, noHost, isNative,
                    isCppMode, archValue, ilcArgsValue, ilcPathValue, ilcSdkPathValue))
                {
                    return false;
                }
            }

            return true;
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
            string ilcPathValue, string ilcSdkPathValue)
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
