// Copyright (c) .NET Foundation and contributors. All rights reserved.
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
            var graphCollector = new ProjectGraphCollector(
                (project, target) => ProjectContext.Create(project, target),
                ResolveProjectFrameworks
                );
            var graph = graphCollector.Collect(files, frameworks).ToArray();
            var builder = new DotNetProjectBuilder((BuilderCommandApp) args);
            return builder.Build(graph).All(r => r != CompilationResult.Failure);
        }

        private static IEnumerable<NuGetFramework> ResolveProjectFrameworks(string projectPath)
        {
            if (!projectPath.EndsWith(Project.FileName))
            {
                projectPath = Path.Combine(projectPath, Project.FileName);
            }
            var project = ProjectReader.GetProject(projectPath);

            return project.GetTargetFrameworks().Select(f => f.FrameworkName);
        }
    }
}
