﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.ProjectModel.Compilation;
using NuGet.Frameworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.DotNet.Tools.Common;
using Microsoft.DotNet.ProjectModel.Utilities;

namespace Microsoft.DotNet.Tools.Publish
{
    public class PublishCommand
    {
        public string ProjectPath { get; set; }
        public string Configuration { get; set; }
        public string OutputPath { get; set; }
        public string Framework { get; set; }
        public string Runtime { get; set; }
        public bool NativeSubdirectories { get; set; }
        public NuGetFramework NugetFramework { get; set; }
        public IEnumerable<ProjectContext> ProjectContexts { get; set; }

        public int NumberOfProjects { get; private set; }
        public int NumberOfPublishedProjects { get; private set; }

        public bool TryPrepareForPublish()
        {
            if (Framework != null)
            {
                NugetFramework = NuGetFramework.Parse(Framework);

                if (NugetFramework.IsUnsupported)
                {
                    Reporter.Output.WriteLine($"Unsupported framework {Framework}.".Red());
                    return false;
                }
            }

            ProjectContexts = SelectContexts(ProjectPath, NugetFramework, Runtime);
            if (!ProjectContexts.Any())
            {
                string errMsg = $"'{ProjectPath}' cannot be published  for '{Framework ?? "<no framework provided>"}' '{Runtime ?? "<no runtime provided>"}'";
                Reporter.Output.WriteLine(errMsg.Red());
                return false;
            }

            return true;
        }

        public void PublishAllProjects()
        {
            NumberOfPublishedProjects = 0;
            NumberOfProjects = 0;

            foreach (var project in ProjectContexts)
            {
                if (PublishProjectContext(project, OutputPath, Configuration, NativeSubdirectories))
                {
                    NumberOfPublishedProjects++;
                }

                NumberOfProjects++;
            }
        }

        /// <summary>
        /// Publish the project for given 'framework (ex - dnxcore50)' and 'runtimeID (ex - win7-x64)'
        /// </summary>
        /// <param name="context">project that is to be published</param>
        /// <param name="outputPath">Location of published files</param>
        /// <param name="configuration">Debug or Release</param>
        /// <returns>Return 0 if successful else return non-zero</returns>
        private static bool PublishProjectContext(ProjectContext context, string outputPath, string configuration, bool nativeSubdirectories)
        {
            Reporter.Output.WriteLine($"Publishing {context.RootProject.Identity.Name.Yellow()} for {context.TargetFramework.DotNetFrameworkName.Yellow()}/{context.RuntimeIdentifier.Yellow()}");

            var options = context.ProjectFile.GetCompilerOptions(context.TargetFramework, configuration);

            // Generate the output path
            if (string.IsNullOrEmpty(outputPath))
            {
                outputPath = Path.Combine(
                    context.ProjectFile.ProjectDirectory,
                    Constants.BinDirectoryName,
                    configuration,
                    context.TargetFramework.GetTwoDigitShortFolderName(),
                    context.RuntimeIdentifier);
            }

            var contextVariables = new Dictionary<string, string>
            {
                { "publish:ProjectPath", context.ProjectDirectory },
                { "publish:Configuration", configuration },
                { "publish:OutputPath", outputPath },
                { "publish:Framework", context.TargetFramework.Framework },
                { "publish:Runtime", context.RuntimeIdentifier },
            };

            RunScripts(context, ScriptNames.PrePublish, contextVariables);

            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            // Compile the project (and transitively, all it's dependencies)
            var result = Command.Create("dotnet-build", 
                new string[] {
                    "--framework", 
                    $"{context.TargetFramework.DotNetFrameworkName}",
                    "--output", 
                    $"{outputPath}",
                    "--configuration", 
                    $"{configuration}",
                    "--no-host",
                    $"{context.ProjectFile.ProjectDirectory}"
                })
                .ForwardStdErr()
                .ForwardStdOut()
                .Execute();

            if (result.ExitCode != 0)
            {
                return false;
            }

            // Use a library exporter to collect publish assets
            var exporter = context.CreateExporter(configuration);

            foreach (var export in exporter.GetAllExports())
            {
                // Skip copying project references
                if (export.Library is ProjectDescription)
                {
                    continue;
                }

                Reporter.Verbose.WriteLine($"Publishing {export.Library.Identity.ToString().Green().Bold()} ...");

                PublishFiles(export.RuntimeAssemblies, outputPath, false);
                PublishFiles(export.NativeLibraries, outputPath, nativeSubdirectories);
            }

            CopyContents(context, outputPath);

            // Publish a host if this is an application
            if (options.EmitEntryPoint.GetValueOrDefault())
            {
                Reporter.Verbose.WriteLine($"Making {context.ProjectFile.Name.Cyan()} runnable ...");
                PublishHost(context, outputPath);
            }

            RunScripts(context, ScriptNames.PostPublish, contextVariables);

            Reporter.Output.WriteLine($"Published to {outputPath}".Green().Bold());

            return true;
        }

        private static int PublishHost(ProjectContext context, string outputPath)
        {
            if (context.TargetFramework.IsDesktop())
            {
                return 0;
            }

            foreach (var binaryName in Constants.HostBinaryNames)
            {
                var hostBinaryPath = Path.Combine(AppContext.BaseDirectory, binaryName);
                if (!File.Exists(hostBinaryPath))
                {
                    Reporter.Error.WriteLine($"Cannot find {binaryName} in the dotnet directory.".Red());
                    return 1;
                }

                var outputBinaryName = binaryName.Equals(Constants.HostExecutableName) ? (context.ProjectFile.Name + Constants.ExeSuffix) : binaryName;
                var outputBinaryPath = Path.Combine(outputPath, outputBinaryName);

                File.Copy(hostBinaryPath, outputBinaryPath, overwrite: true);
            }

            return 0;
        }

        private static void PublishFiles(IEnumerable<LibraryAsset> files, string outputPath, bool nativeSubdirectories)
        {
            foreach (var file in files)
            {
                var destinationDirectory = DetermineFileDestinationDirectory(file, outputPath, nativeSubdirectories);

                if (!Directory.Exists(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }

                File.Copy(file.ResolvedPath, Path.Combine(destinationDirectory, Path.GetFileName(file.ResolvedPath)), overwrite: true);
            }
        }

        private static string DetermineFileDestinationDirectory(LibraryAsset file, string outputPath, bool nativeSubdirectories)
        {
            var destinationDirectory = outputPath;

            if (nativeSubdirectories)
            {
                destinationDirectory = Path.Combine(outputPath, GetNativeRelativeSubdirectory(file.RelativePath));
            }

            return destinationDirectory;
        }

        private static string GetNativeRelativeSubdirectory(string filepath)
        {
            string directoryPath = Path.GetDirectoryName(filepath);

            string[] parts = directoryPath.Split(new string[] { "native" }, 2, StringSplitOptions.None);

            if (parts.Length != 2)
            {
                throw new Exception("Unrecognized Native Directory Format: " + filepath);
            }

            string candidate = parts[1];
            candidate = candidate.TrimStart(new char[] { '/', '\\' });

            return candidate;
        }

        private static IEnumerable<ProjectContext> SelectContexts(string projectPath, NuGetFramework framework, string runtime)
        {
            var allContexts = ProjectContext.CreateContextForEachTarget(projectPath);
            if (string.IsNullOrEmpty(runtime))
            {
                // Nothing was specified, so figure out what the candidate runtime identifiers are and try each of them
                // Temporary until #619 is resolved
                foreach (var candidate in PlatformServices.Default.Runtime.GetAllCandidateRuntimeIdentifiers())
                {
                    var contexts = GetMatchingProjectContexts(allContexts, framework, candidate);
                    if (contexts.Any())
                    {
                        return contexts;
                    }
                }
                return Enumerable.Empty<ProjectContext>();
            }
            else
            {
                return GetMatchingProjectContexts(allContexts, framework, runtime);
            }
        }

        /// <summary>
        /// Return the matching framework/runtime ProjectContext.
        /// If 'framework' or 'runtimeIdentifier' is null or empty then it matches with any.
        /// </summary>
        private static IEnumerable<ProjectContext> GetMatchingProjectContexts(IEnumerable<ProjectContext> contexts, NuGetFramework framework, string runtimeIdentifier)
        {
            foreach (var context in contexts)
            {
                if (context.TargetFramework == null || string.IsNullOrEmpty(context.RuntimeIdentifier))
                {
                    continue;
                }

                if (string.IsNullOrEmpty(runtimeIdentifier) || string.Equals(runtimeIdentifier, context.RuntimeIdentifier, StringComparison.OrdinalIgnoreCase))
                {
                    if (framework == null || framework.Equals(context.TargetFramework))
                    {
                        yield return context;
                    }
                }
            }
        }

        private static void CopyContents(ProjectContext context, string outputPath)
        {
            var contentFiles = context.ProjectFile.Files.GetContentFiles();
            Copy(contentFiles, context.ProjectDirectory, outputPath);
        }

        private static void Copy(IEnumerable<string> contentFiles, string sourceDirectory, string targetDirectory)
        {
            if (contentFiles == null)
            {
                throw new ArgumentNullException(nameof(contentFiles));
            }

            sourceDirectory = PathUtility.EnsureTrailingSlash(sourceDirectory);
            targetDirectory = PathUtility.EnsureTrailingSlash(targetDirectory);

            foreach (var contentFilePath in contentFiles)
            {
                Reporter.Verbose.WriteLine($"Publishing {contentFilePath.Green().Bold()} ...");

                var fileName = Path.GetFileName(contentFilePath);

                var targetFilePath = contentFilePath.Replace(sourceDirectory, targetDirectory);
                var targetFileParentFolder = Path.GetDirectoryName(targetFilePath);

                // Create directory before copying a file
                if (!Directory.Exists(targetFileParentFolder))
                {
                    Directory.CreateDirectory(targetFileParentFolder);
                }

                File.Copy(
                    contentFilePath,
                    targetFilePath,
                    overwrite: true);

                // clear read-only bit if set
                var fileAttributes = File.GetAttributes(targetFilePath);
                if ((fileAttributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    File.SetAttributes(targetFilePath, fileAttributes & ~FileAttributes.ReadOnly);
                }
            }
        }

        private static void RunScripts(ProjectContext context, string name, Dictionary<string, string> contextVariables)
        {
            foreach (var script in context.ProjectFile.Scripts.GetOrEmpty(name))
            {
                ScriptExecutor.CreateCommandForScript(context.ProjectFile, script, contextVariables)
                    .ForwardStdErr()
                    .ForwardStdOut()
                    .Execute();
            }
        }
    }
}
