// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.ProjectModel;
using NuGet.Frameworks;

namespace Microsoft.DotNet.Cli.Compiler.Common
{
    public static class ProjectContextExtensions
    {
        public static string ProjectName(this Project project) => project.Name;

        public static string GetDisplayName(this Project project, NuGetFramework framework) => $"{project.Name} ({framework})";

        public static CommonCompilerOptions GetLanguageSpecificCompilerOptions(this ProjectContext context, string configurationName)
        {
            return context.ProjectFile.GetLanguageSpecificCompilerOptions(context.TargetFramework, configurationName);
        }

        public static CommonCompilerOptions GetLanguageSpecificCompilerOptions(this Project project, NuGetFramework framework, string configurationName)
        {
            var baseOption = project.GetCompilerOptions(framework, configurationName);

            IReadOnlyList<string> defaultSuppresses;
            var compilerName = project.CompilerName ?? "csc";
            if (DefaultCompilerWarningSuppresses.Suppresses.TryGetValue(compilerName, out defaultSuppresses))
            {
                baseOption.SuppressWarnings = (baseOption.SuppressWarnings ?? Enumerable.Empty<string>()).Concat(defaultSuppresses).Distinct();
            }

            return baseOption;
        }

        public static string GetSDKVersionFile(this Project project, NuGetFramework targetFramework, string configuration, string buildBasePath, string outputPath)
        {
            var intermediatePath = project.GetOutputPaths(targetFramework, configuration, buildBasePath, outputPath).IntermediateOutputDirectoryPath;
            return Path.Combine(intermediatePath, ".SDKVersion");
        }

        public static string IncrementalCacheFile(this Project project, NuGetFramework targetFramework, string configuration, string buildBasePath, string outputPath)
        {
            var intermediatePath = project.GetOutputPaths(targetFramework, configuration, buildBasePath, outputPath).IntermediateOutputDirectoryPath;
            return Path.Combine(intermediatePath, ".IncrementalCache");
        }

        // used in incremental compilation for the key file
        public static CommonCompilerOptions ResolveCompilationOptions(this Project project, NuGetFramework targetFramework, string configuration)
        {
            var compilationOptions = project.GetLanguageSpecificCompilerOptions(targetFramework, configuration);

            // Path to strong naming key in environment variable overrides path in project.json
            var environmentKeyFile = Environment.GetEnvironmentVariable(EnvironmentNames.StrongNameKeyFile);

            if (!string.IsNullOrWhiteSpace(environmentKeyFile))
            {
                compilationOptions.KeyFile = environmentKeyFile;
            }
            else if (!string.IsNullOrWhiteSpace(compilationOptions.KeyFile))
            {
                // Resolve full path to key file
                compilationOptions.KeyFile =
                    Path.GetFullPath(Path.Combine(project.ProjectDirectory, compilationOptions.KeyFile));
            }
            return compilationOptions;
        }

        public static OutputPaths GetOutputPaths(this Project project, NuGetFramework targetFramework, string configuration, string buidBasePath = null, string outputPath = null)
        {
            return OutputPathsCalculator.GetOutputPaths(project,
                targetFramework,
                null,
                configuration,
                ProjectRootResolver.ResolveRootDirectory(project.ProjectDirectory),
                buidBasePath,
                outputPath);
        }
    }
}
