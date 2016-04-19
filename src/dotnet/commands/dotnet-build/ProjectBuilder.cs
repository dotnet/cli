using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.ProjectModel;
using NuGet.Frameworks;

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
            CompilationResult result;
            if (_compilationResults.TryGetValue(projectNode.ProjectContext.Identity, out result))
            {
                return result;
            }
            result = CompileWithDependencies(projectNode);

            _compilationResults[projectNode.ProjectContext.Identity] = result;

            return result;
        }

        private CompilationResult CompileWithDependencies(ProjectGraphNode projectNode)
        {
            var skipIncrementalCheck = false;
            if (!_skipDependencies)
            {
                foreach (var dependency in projectNode.Dependencies)
                {
                    var context = dependency.ProjectContext;
                    if (!context.ProjectFile.Files.SourceFiles.Any())
                    {
                        continue;
                    }
                    var result = Build(dependency);
                    switch (result)
                    {
                        case CompilationResult.IncrementalSkip:
                            break;
                        case CompilationResult.Success:
                            skipIncrementalCheck = true;
                            break;
                        case CompilationResult.Failure:
                            return CompilationResult.Failure;
                        default:
                            throw new NotSupportedException();
                    }
                }
            }
            if (skipIncrementalCheck || NeedsRebuilding(projectNode))
            {
                return RunCompile(projectNode);
            }
            else
            {
                return CompilationResult.IncrementalSkip;
            }
        }

        protected abstract CompilationResult RunCompile(ProjectGraphNode projectNode);

        protected abstract bool NeedsRebuilding(ProjectGraphNode projectNode);
    }
}