using System;
using System.Collections.Generic;
using NuGet.Frameworks;
using Microsoft.DotNet.ProjectModel;

namespace Microsoft.DotNet.Tools.Build
{
    internal enum CompilationResult
    {
        IncrementalSkip, Success, Failure
    }

    internal abstract class ProjectBuilder
    {
        private readonly bool _skipDependencies;

        public ProjectBuilder(bool skipDependencies)
        {
            _skipDependencies = skipDependencies;
        }

        private Dictionary<ProjectContextIdentity, CompilationResult> _compilationResults = new Dictionary<ProjectContextIdentity, CompilationResult>();

        public IEnumerable<CompilationResult> Build(IEnumerable<ProjectGraphNode> roots)
        {
            foreach (var projectNode in roots)
            {
                yield return Build(projectNode);
            }
        }

        private CompilationResult Build(ProjectGraphNode projectNode)
        {
            var projectContextIdentity = new ProjectContextIdentity(projectNode.ProjectContext);

            CompilationResult result;
            if (_compilationResults.TryGetValue(projectContextIdentity, out result))
            {
                return result;
            }
            result = CompileWithDependencies(projectNode);

            _compilationResults[projectContextIdentity] = result;

            return result;
        }

        private CompilationResult CompileWithDependencies(ProjectGraphNode projectNode)
        {
            var skipIncrementalCheck = false;
            if (!_skipDependencies)
            {
                foreach (var dependency in projectNode.Dependencies)
                {
                    var result = Build(dependency);
                    switch (result)
                    {
                        case CompilationResult.IncrementalSkip:
                            break;
                        case CompilationResult.Success:
                            skipIncrementalCheck = false;
                            break;
                        case CompilationResult.Failure:
                            return CompilationResult.Failure;
                        default:
                            throw new NotSupportedException();
                    }
                }
            }
            if (CheckIncremental(projectNode))
            {
                return RunCompile(projectNode);
            }
            else
            {
                return CompilationResult.IncrementalSkip;
            }
        }

        protected abstract CompilationResult RunCompile(ProjectGraphNode projectNode);

        protected abstract bool CheckIncremental(ProjectGraphNode projectNode);

        private CompilationResult CompileDependency(ProjectGraphNode dependency)
        {
            throw new NotImplementedException();
        }


        private class ProjectContextIdentity : Tuple<string, NuGetFramework>
        {
            public ProjectContextIdentity(ProjectContext context) : base(context.ProjectFile?.ProjectFilePath, context.TargetFramework)
            {
            }
        }
    }
}