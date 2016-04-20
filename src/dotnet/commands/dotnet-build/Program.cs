﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.Tools.Compiler;
using Microsoft.DotNet.Cli.Utils;
using NuGet.Frameworks;

namespace Microsoft.DotNet.Tools.Build
{
    public class BuildCommand
    {
        public static int Run(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);

            try
            {
                var app = new BuilderCommandApp("dotnet build", ".NET Builder", "Builder for the .NET Platform. It performs incremental compilation if it's safe to do so. Otherwise it delegates to dotnet-compile which performs non-incremental compilation");
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

        private static bool OnExecute(IEnumerable<string> files, IEnumerable<NuGetFramework> frameworks, CompilerCommandApp args)
        {
            var builderCommandApp = (BuilderCommandApp)args;
            var graphCollector = new ProjectGraphCollector(
                !builderCommandApp.ShouldSkipDependencies,
                (project, target) => ProjectContext.Create(project, target));

            var contexts = ResolveRootContexts(files, frameworks, args);
            var graph = graphCollector.Collect(contexts).ToArray();
            var builder = new DotNetProjectBuilder(builderCommandApp);
            return builder.Build(graph).ToArray().All(r => r != CompilationResult.Failure);
        }

        private static IEnumerable<ProjectContext> ResolveRootContexts(
            IEnumerable<string> files,
            IEnumerable<NuGetFramework> frameworks,
            CompilerCommandApp args)
        {

            // Set defaults based on the environment
            var settings = ProjectReaderSettings.ReadFromEnvironment();

            if (!string.IsNullOrEmpty(args.VersionSuffixValue))
            {
                settings.VersionSuffix = args.VersionSuffixValue;
            }

            foreach (var file in files)
            {
                var project = ProjectReader.GetProject(file);
                var allFrameworks = project.GetTargetFrameworks().Select(f => f.FrameworkName);
                if (!allFrameworks.Any())
                {
                    throw new InvalidOperationException(
                        $"Project '{file}' does not have any frameworks listed in the 'frameworks' section.");
                }
                IEnumerable<NuGetFramework> selectedFrameworks;
                if (frameworks != null)
                {
                    selectedFrameworks = allFrameworks.Where(f => frameworks.Any(ff => ff.Equals(f)));
                }
                else
                {
                    selectedFrameworks = allFrameworks;
                }

                if (!selectedFrameworks.Any())
                {
                    throw new InvalidOperationException(
                        $"Project \'{file}\' does not support framework: {string.Join(", ", frameworks.Select(fx => fx.DotNetFrameworkName))}.");
                }

                foreach (var framework in selectedFrameworks)
                {
                    yield return new ProjectContextBuilder()
                        .WithProjectDirectory(Path.GetDirectoryName(file))
                        .WithTargetFramework(framework)
                        .WithReaderSettings(settings)
                        .Build();
                }
            }
        }
    }
}
