﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.ProjectModel.Utilities;
using Microsoft.Extensions.FileSystemGlobbing;

namespace Microsoft.DotNet.ProjectModel.Files
{
    public class IncludeFilesResolver
    {
        public static IEnumerable<IncludeEntry> GetIncludeFiles(IncludeContext context, string targetBasePath, IList<DiagnosticMessage> diagnostics)
        {
            return GetIncludeFiles(context, targetBasePath, diagnostics, flatten: false);
        }

        public static IEnumerable<IncludeEntry> GetIncludeFiles(
            IncludeContext context,
            string targetBasePath,
            IList<DiagnosticMessage> diagnostics,
            bool flatten)
        {
            var sourceBasePath = PathUtility.EnsureTrailingSlash(context.SourceBasePath);
            targetBasePath = PathUtility.GetPathWithDirectorySeparator(targetBasePath);

            var includeEntries = new HashSet<IncludeEntry>();

            // Check for illegal characters in target path
            if (string.IsNullOrEmpty(targetBasePath))
            {
                diagnostics?.Add(new DiagnosticMessage(
                    ErrorCodes.NU1003,
                    $"Invalid '{context.Option}' section. The target '{targetBasePath}' is invalid, " +
                    "targets must either be a file name or a directory suffixed with '/'. " +
                    "The root directory of the package can be specified by using a single '/' character.",
                    sourceBasePath,
                    DiagnosticMessageSeverity.Error));
            }
            else if (targetBasePath.Split('/').Any(s => s.Equals(".") || s.Equals("..")))
            {
                diagnostics?.Add(new DiagnosticMessage(
                    ErrorCodes.NU1004,
                    $"Invalid '{context.Option}' section. " +
                    $"The target '{targetBasePath}' contains path-traversal characters ('.' or '..'). " +
                    "These characters are not permitted in target paths.",
                    sourceBasePath,
                    DiagnosticMessageSeverity.Error));
            }
            else
            {
                var files = GetIncludeFilesCore(
                    sourceBasePath,
                    context.IncludePatterns,
                    context.ExcludePatterns,
                    context.IncludeFiles,
                    context.ExcludeFiles,
                    context.BuiltInsInclude,
                    context.BuiltInsExclude);

                var isFile = !targetBasePath.EndsWith(Path.DirectorySeparatorChar.ToString());
                if (isFile && files.Count() > 1)
                {
                    // It's a file. But the glob matched multiple things
                    diagnostics?.Add(new DiagnosticMessage(
                        ErrorCodes.NU1005,
                        $"Invalid '{ProjectFilesCollection.PackIncludePropertyName}' section. " +
                        $"The target '{targetBasePath}' refers to a single file, but the corresponding pattern " +
                        "produces multiple files. To mark the target as a directory, suffix it with '/'.",
                        sourceBasePath,
                        DiagnosticMessageSeverity.Error));
                }
                else if (isFile && files.Any())
                {
                    includeEntries.Add(new IncludeEntry(targetBasePath, files.First()));
                }
                else
                {
                    targetBasePath = targetBasePath.Substring(0, targetBasePath.Length - 1);

                    foreach (var file in files)
                    {
                        string targetPath = null;

                        if (flatten)
                        {
                            targetPath = Path.Combine(targetBasePath, Path.GetFileName(file));
                        }
                        else
                        {
                            targetPath = Path.Combine(targetBasePath, PathUtility.GetRelativePath(sourceBasePath, file));
                        }

                        includeEntries.Add(new IncludeEntry(targetPath, file));
                    }
                }
            }
            
            if (context.Mappings != null)
            {
                // Finally add all the mappings
                foreach (var map in context.Mappings)
                {
                    var targetPath = Path.Combine(targetBasePath, PathUtility.GetPathWithDirectorySeparator(map.Key));

                    foreach (var file in GetIncludeFiles(map.Value, targetPath, diagnostics, flatten))
                    {
                        file.IsCustomTarget = true;

                        // Prefer named targets over default ones
                        includeEntries.RemoveWhere(f => string.Equals(f.SourcePath, file.SourcePath));
                        includeEntries.Add(file);
                    }
                }
            }

            return includeEntries;
        }

        private static IEnumerable<string> GetIncludeFilesCore(
            string sourceBasePath,
            List<string> includePatterns,
            List<string> excludePatterns,
            List<string> includeFiles,
            List<string> excludeFiles,
            List<string> builtInsInclude,
            List<string> builtInsExclude)
        {
            var literalIncludedFiles = new List<string>();

            if (includeFiles != null)
            {
                // literal included files are added at the last, but the search happens early
                // so as to make the process fail early in case there is missing file. fail early
                // helps to avoid unnecessary globing for performance optimization
                foreach (var literalRelativePath in includeFiles)
                {
                    var fullPath = Path.GetFullPath(Path.Combine(sourceBasePath, literalRelativePath));

                    if (!File.Exists(fullPath))
                    {
                        throw new InvalidOperationException(string.Format("Can't find file {0}", literalRelativePath));
                    }

                    literalIncludedFiles.Add(fullPath);
                }
            }

            // Globbing
            var matcher = new Matcher();
            if (builtInsInclude != null)
            {
                matcher.AddIncludePatterns(builtInsInclude);
            }
            if (includePatterns != null)
            {
                matcher.AddIncludePatterns(includePatterns);
            }
            if (builtInsExclude != null)
            {
                matcher.AddExcludePatterns(builtInsExclude);
            }
            if (excludePatterns != null)
            {
                matcher.AddExcludePatterns(excludePatterns);
            }

            var files = matcher.GetResultsInFullPath(sourceBasePath);
            files = files.Concat(literalIncludedFiles).Distinct();

            if (files.Count() > 0 && excludeFiles != null)
            {
                var literalExcludedFiles = excludeFiles.Select(file => Path.GetFullPath(Path.Combine(sourceBasePath, file)));
                files = files.Except(literalExcludedFiles);
            }

            return files;
        }
    }
}
