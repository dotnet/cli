// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.Tools.Compiler;
using Microsoft.DotNet.ProjectModel.Graph;
using NuGet.Frameworks;

namespace Microsoft.DotNet.Tools.Build
{
    public class ProjectGraphCollector
    {
        private readonly Func<string, NuGetFramework, ProjectContext> _projectContextFactory;

        public ProjectGraphCollector(Func<string, NuGetFramework, ProjectContext> projectContextFactory)
        {
            _projectContextFactory = projectContextFactory;
        }

        public IEnumerable<ProjectGraphNode> Collect(IEnumerable<string> projects, IEnumerable<string> frameworks)
        {
            foreach (var project in projects)
            {
                foreach (var framework in frameworks)
                {
                    var context = _projectContextFactory(project, NuGetFramework.Parse(framework));
                    var libraries = context.LibraryManager.GetLibraries();
                    var lookup = libraries.ToDictionary(l => l.Identity.Name);
                    var root = lookup[context.ProjectFile.Name];
                    yield return TraverseProject(root, lookup, context);
                }
            }   
        }

        private ProjectGraphNode TraverseProject(LibraryDescription root, IDictionary<string, LibraryDescription> lookup, ProjectContext context = null)
        {
            var deps = new List<ProjectGraphNode>();
            foreach (var dependency in root.Dependencies)
            {
                if (dependency.Type.Equals(LibraryType.Project))
                {
                    deps.Add(TraverseProject(lookup[dependency.Name], lookup));
                }
                else
                {
                    deps.AddRange(TraversePackage(lookup[dependency.Name], lookup));
                }
            }
            if (context != null)
            {
                return new ProjectGraphNode(()=>context, deps)
            }
            else
            {
                return new ProjectGraphNode(()=>_projectContextFactory(root.Path, root.Framework), deps);
            }
        }

        private IEnumerable<ProjectGraphNode> TraversePackage(LibraryDescription root, IDictionary<string, LibraryDescription> lookup)
        {
            foreach (var dependency in root.Dependencies)
            {
                if (dependency.Type.Equals(LibraryType.Project))
                {
                    yield return TraverseProject(lookup[dependency.Name], lookup);
                }
                else
                {
                    foreach(var node in TraversePackage(lookup[dependency.Name], lookup))
                    {
                        yield return node;
                    }
                }
            }
        }
    }

    public class ProjectGraphNode
    {
        Func<ProjectContext> _projectContextCreator;

        public ProjectGraphNode(Func<ProjectContext> projectContextCreator, IEnumerable<ProjectGraphNode> dependencies)
        {
            _projectContextCreator = projectContextCreator;
            Dependencies = dependencies;
        }

        public ProjectContext ProjectContext { get { return _projectContextCreator(); } }

        public IEnumerable<ProjectGraphNode> Dependencies { get; }
    }

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

        private static bool OnExecute(List<ProjectContext> contexts, CompilerCommandApp args)
        {
            var compileContexts = contexts.Select(context => new CompileContext(context, (BuilderCommandApp)args)).ToList();

            var incrementalSafe = compileContexts.All(c => c.IsSafeForIncrementalCompilation);

            return compileContexts.All(c => c.Compile(incrementalSafe));
        }
    }
}
