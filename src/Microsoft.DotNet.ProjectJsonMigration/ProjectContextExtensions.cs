// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.Build.Construction;
using System.Linq;
using Microsoft.DotNet.Internal.ProjectModel;
using System.IO;

namespace Microsoft.DotNet.ProjectJsonMigration
{
    internal static class ProjectContextExtensions
    {
        public static string GetProjectName(this ProjectContext projectContext)
        {
            var projectDirectory = projectContext.ProjectDirectory;
            
            return Path.GetFileName(projectDirectory);
        }

        public static bool HasRuntimes(this IEnumerable<ProjectContext> projectContexts)
        {
            return projectContexts.Any(p => p.ProjectFile.Runtimes.Any());
        }

        public static bool HasBothCoreAndFullFrameworkTFMs(this IEnumerable<ProjectContext> projectContexts)
        {
            return projectContexts.HasCoreTFM() && projectContexts.HasFullFrameworkTFM();
        }

        public static bool HasCoreTFM(this IEnumerable<ProjectContext> projectContexts)
        {
            return projectContexts.Any(p => !p.IsFullFramework());
        }

        public static bool HasFullFrameworkTFM(this IEnumerable<ProjectContext> projectContexts)
        {
            return projectContexts.Any(p => p.IsFullFramework());
        }

        public static bool HasExeOutput(this IEnumerable<ProjectContext> projectContexts)
        {
            return projectContexts.Any(p => p.IsExe());
        }

        public static bool HasLibraryOutput(this IEnumerable<ProjectContext> projectContexts)
        {
            return projectContexts.Any(p => p.IsLibrary());
        }

        public static bool IsFullFramework(this ProjectContext projectContext)
        {
            return !projectContext.TargetFramework.IsPackageBased;
        }

        public static bool IsExe(this ProjectContext projectContext)
        {
            var compilerOptions = projectContext.ProjectFile.GetCompilerOptions(null, null);
            return (compilerOptions.EmitEntryPoint != null && compilerOptions.EmitEntryPoint.Value);
        }

        public static bool IsLibrary(this ProjectContext projectContext)
        {
            var compilerOptions = projectContext.ProjectFile.GetCompilerOptions(null, null);
            return (compilerOptions.EmitEntryPoint == null || !compilerOptions.EmitEntryPoint.Value);
        }
    }
}
