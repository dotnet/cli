// Copyright (c) .NET Foundation and contributors. All rights reserved.
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
        private string _sourceBasePath;
        private string _option;
        private List<string> _includePatterns;
        private List<string> _excludePatterns;
        private List<string> _includeFiles;
        private List<string> _excludeFiles;
        private List<string> _builtInsInclude;
        private List<string> _builtInsExclude;
        private IEnumerable<KeyValuePair<string, IncludeFilesResolver>> _mappings;
        private IEnumerable<string> _resolvedIncludeFiles;

        public IncludeFilesResolver(IncludeContext context)
        {
            _sourceBasePath = context.SourceBasePath;
            _option = context.Option;
            _includePatterns = context.IncludePatterns;
            _excludePatterns = context.ExcludePatterns;
            _includeFiles = context.IncludeFiles;
            _excludeFiles = context.ExcludeFiles;
            _builtInsInclude = context.BuiltInsInclude;
            _builtInsExclude = context.BuiltInsExclude;
            _mappings = context.Mappings?.Select(
                map => new KeyValuePair<string, IncludeFilesResolver>(map.Key, new IncludeFilesResolver(map.Value)));
        }

        public List<DiagnosticMessage> Diagnostics { get; } = new List<DiagnosticMessage>();

        public IEnumerable<IncludeEntry> GetIncludeFiles(string targetBasePath, bool flatten=false)
        {
            Diagnostics.Clear();
            _sourceBasePath = PathUtility.EnsureTrailingSlash(_sourceBasePath);
            targetBasePath = PathUtility.GetPathWithDirectorySeparator(targetBasePath);
            var includeFiles = new HashSet<IncludeEntry>();

            // Check for illegal characters in target path
            if (string.IsNullOrEmpty(targetBasePath))
            {
                Diagnostics.Add(new DiagnosticMessage(
                    ErrorCodes.NU1003,
                    $"Invalid '{_option}' section. The target '{targetBasePath}' is invalid, " +
                    "targets must either be a file name or a directory suffixed with '/'. " +
                    "The root directory of the package can be specified by using a single '/' character.",
                    _sourceBasePath,
                    DiagnosticMessageSeverity.Error));
            }
            else if (targetBasePath.Split('/').Any(s => s.Equals(".") || s.Equals("..")))
            {
                Diagnostics.Add(new DiagnosticMessage(
                    ErrorCodes.NU1004,
                    $"Invalid '{_option}' section. " +
                    $"The target '{targetBasePath}' contains path-traversal characters ('.' or '..'). " +
                    "These characters are not permitted in target paths.",
                    _sourceBasePath,
                    DiagnosticMessageSeverity.Error));
            }
            else
            {
                var files = GetIncludeFilesCore();
                var isFile = !targetBasePath.EndsWith(Path.DirectorySeparatorChar.ToString());
                if (isFile && files.Count() > 1)
                {
                    // It's a file. But the glob matched multiple things
                    Diagnostics.Add(new DiagnosticMessage(
                        ErrorCodes.NU1005,
                        $"Invalid '{ProjectFilesCollection.PackIncludePropertyName}' section. " +
                        $"The target '{targetBasePath}' refers to a single file, but the corresponding pattern " +
                        "produces multiple files. To mark the target as a directory, suffix it with '/'.",
                        _sourceBasePath,
                        DiagnosticMessageSeverity.Error));
                }
                else if (isFile && files.Any())
                {
                    includeFiles.Add(new IncludeEntry(targetBasePath, files.First()));
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
                            targetPath = Path.Combine(targetBasePath, PathUtility.GetRelativePath(_sourceBasePath, file));
                        }

                        includeFiles.Add(new IncludeEntry(targetPath, file));
                    }
                }
            }
            
            if (_mappings != null)
            {
                // Finally add all the mappings
                foreach (var map in _mappings)
                {
                    var targetPath = Path.Combine(targetBasePath, PathUtility.GetPathWithDirectorySeparator(map.Key));
                    foreach (var file in map.Value.GetIncludeFiles(targetPath, flatten))
                    {
                        file.IsCustomTarget = true;

                        // Prefer named targets over default ones
                        includeFiles.RemoveWhere(f => string.Equals(f.SourcePath, file.SourcePath));
                        includeFiles.Add(file);
                    }
                    Diagnostics.AddRange(map.Value.Diagnostics);
                }
            }

            return includeFiles;
        }

        private IEnumerable<string> GetIncludeFilesCore()
        {
            if (_resolvedIncludeFiles != null)
            {
                return _resolvedIncludeFiles;
            }

            var literalIncludedFiles = new List<string>();
            if (_includeFiles != null)
            {
                // literal included files are added at the last, but the search happens early
                // so as to make the process fail early in case there is missing file. fail early
                // helps to avoid unnecessary globing for performance optimization
                foreach (var literalRelativePath in _includeFiles)
                {
                    var fullPath = Path.GetFullPath(Path.Combine(_sourceBasePath, literalRelativePath));
                    if (!File.Exists(fullPath))
                    {
                        throw new InvalidOperationException(string.Format("Can't find file {0}", literalRelativePath));
                    }
                    literalIncludedFiles.Add(fullPath);
                }
            }

            // Globbing
            var matcher = new Matcher();
            if (_builtInsInclude != null)
            {
                matcher.AddIncludePatterns(_builtInsInclude);
            }
            if (_includePatterns != null)
            {
                matcher.AddIncludePatterns(_includePatterns);
            }
            if (_builtInsExclude != null)
            {
                matcher.AddExcludePatterns(_builtInsExclude);
            }
            if (_excludePatterns != null)
            {
                matcher.AddExcludePatterns(_excludePatterns);
            }

            var files = matcher.GetResultsInFullPath(_sourceBasePath);
            files = files.Concat(literalIncludedFiles).Distinct();

            if (files.Count() > 0 && _excludeFiles != null)
            {
                var literalExcludedFiles = _excludeFiles.Select(file => Path.GetFullPath(Path.Combine(_sourceBasePath, file)));
                files = files.Except(literalExcludedFiles);
            }

            _resolvedIncludeFiles = files;

            return files;
        }
    }
}
