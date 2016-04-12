// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.ProjectModel.Utilities;
using Microsoft.Extensions.FileSystemGlobbing;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.ProjectModel.Files
{
    public class IncludeFilesResolver
    {
        private static readonly char[] PatternSeparator = new[] { ';' };

        private List<string> _includePatterns;
        private List<string> _excludePatterns;
        private List<string> _includeFiles;
        private List<string> _excludeFiles;
        private List<string> _builtInsInclude;
        private List<string> _builtInsExclude;
        private IDictionary<string, IncludeFilesResolver> _mappings;
        private string _sourceBasePath;
        private string _option;
        private IEnumerable<string> _resolvedIncludeFiles;

        public IncludeFilesResolver(
            string sourceBasePath,
            string option,
            JObject rawObject,
            string[] defaultBuiltInInclude = null,
            string[] defaultBuiltInExclude = null)
        {
            _sourceBasePath = sourceBasePath;
            _option = option;
            var token = rawObject.Value<JToken>(option);
            if (token.Type != JTokenType.Object)
            {
                _includePatterns = new List<string>(ExtractValues(token));
            }
            else
            {
                _includePatterns = CreateCollection(sourceBasePath, "include", ExtractValues(token.Value<JToken>("include")), literalPath: false);
                _excludePatterns = CreateCollection(sourceBasePath, "exclude", ExtractValues(token.Value<JToken>("exclude")), literalPath: false);
                _includeFiles = CreateCollection(sourceBasePath, "includeFiles", ExtractValues(token.Value<JToken>("includeFiles")), literalPath: true);
                _excludeFiles = CreateCollection(sourceBasePath, "excludeFiles", ExtractValues(token.Value<JToken>("excludeFiles")), literalPath: true);
                var builtIns = token.Value<JToken>("builtIns") as JObject;
                if (builtIns != null)
                {
                    _builtInsInclude = CreateCollection(sourceBasePath, "include", ExtractValues(builtIns.Value<JToken>("include")), literalPath: false);
                    if (defaultBuiltInInclude != null && !_builtInsInclude.Any())
                    {
                        _builtInsInclude = defaultBuiltInInclude.ToList();
                    }
                    _builtInsExclude = CreateCollection(sourceBasePath, "exclude", ExtractValues(builtIns.Value<JToken>("exclude")), literalPath: false);
                    if (defaultBuiltInExclude != null && !_builtInsExclude.Any())
                    {
                        _builtInsExclude = defaultBuiltInExclude.ToList();
                    }
                }
                var mappings = token.Value<JToken>("mappings") as JObject;
                if (mappings != null)
                {
                    _mappings = new Dictionary<string, IncludeFilesResolver>();
                    foreach (var map in mappings)
                    {
                        _mappings.Add(map.Key, new IncludeFilesResolver(sourceBasePath, map.Key, mappings));
                    }
                }
            }
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

        private static string[] ExtractValues(JToken token)
        {
            if (token != null)
            {
                if (token.Type == JTokenType.String)
                {
                    return new string[] { token.Value<string>() };
                }

                else if (token.Type == JTokenType.Array)
                {
                    return token.Values<string>().ToArray();
                }
            }

            return new string[0];
        }

        private static List<string> CreateCollection(string projectDirectory, string propertyName, IEnumerable<string> patternsStrings, bool literalPath)
        {
            var patterns = patternsStrings.SelectMany(patternsString => GetSourcesSplit(patternsString))
                                          .Select(patternString => patternString.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar));

            foreach (var pattern in patterns)
            {
                if (Path.IsPathRooted(pattern))
                {
                    throw new InvalidOperationException($"The '{propertyName}' property cannot be a rooted path.");
                }

                if (literalPath && pattern.Contains('*'))
                {
                    throw new InvalidOperationException($"The '{propertyName}' property cannot contain wildcard characters.");
                }
            }

            return new List<string>(patterns.Select(pattern => FolderToPattern(pattern, projectDirectory)));
        }

        private static IEnumerable<string> GetSourcesSplit(string sourceDescription)
        {
            if (string.IsNullOrEmpty(sourceDescription))
            {
                return Enumerable.Empty<string>();
            }

            return sourceDescription.Split(PatternSeparator, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string FolderToPattern(string candidate, string projectDir)
        {
            // This conversion is needed to support current template

            // If it's already a pattern, no change is needed
            if (candidate.Contains('*'))
            {
                return candidate;
            }

            // If the given string ends with a path separator, or it is an existing directory
            // we convert this folder name to a pattern matching all files in the folder
            if (candidate.EndsWith(@"\") ||
                candidate.EndsWith("/") ||
                Directory.Exists(Path.Combine(projectDir, candidate)))
            {
                return Path.Combine(candidate, "**", "*");
            }

            // Otherwise, it represents a single file
            return candidate;
        }
    }
}
