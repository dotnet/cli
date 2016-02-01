// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.Extensions.DependencyModel
{
    public class CompilationLibrary : Library
    {
        private static Lazy<Assembly> _entryAssembly = new Lazy<Assembly>(GetEntryAssembly);

        private static string _nugetPackages = Environment.GetEnvironmentVariable("NUGET_PACKAGES") ?? GetDefaultPackageDirectory();

        private static string _packageCache = Environment.GetEnvironmentVariable("DOTNET_PACKAGES_CACHE");

        public CompilationLibrary(string libraryType, string packageName, string version, string hash, string[] assemblies, Dependency[] dependencies, bool serviceable)
            : base(libraryType, packageName, version, hash,  dependencies, serviceable)
        {
            Assemblies = assemblies;
        }

        public IReadOnlyList<string> Assemblies { get; }

        public IEnumerable<string> ResolveReferencePaths()
        {
            var entryAssembly = _entryAssembly.Value;

            string basePath;

            var appBase = Path.GetDirectoryName(entryAssembly.Location);
            var refsDir = Path.Combine(appBase, "refs");

            var hasRefs = Directory.Exists(refsDir);
            var isProject = string.Equals(LibraryType, "project", StringComparison.OrdinalIgnoreCase);
            // 1. Try searching for package in package cache for non-project references
            if (!isProject && TryResolvePackageCache(out basePath))
            {
                return ResolveAssembliesFromPackagePath(basePath);
            }
            // 2. Try searching localy if refs folder availible
            if (hasRefs || isProject)
            {
                var directories = new List<string>()
                {
                    appBase
                };

                if (hasRefs)
                {
                    directories.Add(refsDir);
                }
                return ResolveAssembliesFromDirectories(directories.ToArray());
            }
            // 3. Try searching in package directory
            if (TryResolvePackagePath(out basePath))
            {
                return ResolveAssembliesFromPackagePath(basePath);
            }

            throw new InvalidOperationException($"Can not find compilation library location for package '{PackageName}'");
        }

        private IEnumerable<string> ResolveAssembliesFromDirectories(string[] directories)
        {
            foreach (var assembly in Assemblies)
            {
                var assemblyFile = Path.GetFileName(assembly);
                foreach (var directory in directories)
                {
                    string fullName;
                    if (TryResolveAssemblyFile(directory, assemblyFile, out fullName))
                    {
                        yield return fullName;
                        break;
                    }

                    var errorMessage = $"Can not find assembly file {assemblyFile} at '{string.Join(",", directories)}'";
                    throw new InvalidOperationException(errorMessage);
                }
            }
        }

        private IEnumerable<string> ResolveAssembliesFromPackagePath(string basePath)
        {
            foreach (var assembly in Assemblies)
            {
                string fullName;
                if (!TryResolveAssemblyFile(basePath, assembly, out fullName))
                {
                    throw new InvalidOperationException($"Can not find assembly file at '{fullName}'");
                }
                yield return fullName;
            }
        }

        private bool TryResolveAssemblyFile(string basePath, string assemblyPath, out string fullName)
        {
            fullName = Path.Combine(basePath, assemblyPath);
            if (File.Exists(fullName))
            {
                return true;
            }
            return false;
        }

        private bool TryResolvePackagePath(out string packagePath)
        {
            packagePath = string.Empty;

            if (!string.IsNullOrEmpty(_nugetPackages) &&
                TryResolvePackagePath(_nugetPackages, out packagePath))
            {
                return true;
            }
            return false;
        }

        private bool TryResolvePackageCache(out string packagePath)
        {
            packagePath = string.Empty;

            if (!string.IsNullOrEmpty(_packageCache))
            {
                var hashSplitterPos = Hash.IndexOf('-');
                if (hashSplitterPos <= 0 || hashSplitterPos == Hash.Length - 1)
                {
                    throw new InvalidOperationException($"Invalid hash entry '{Hash}' for package '{PackageName}'");
                }

                var hashAlgorithm = Hash.Substring(0, hashSplitterPos);

                var cacheHashPath = Path.Combine(_packageCache, $"{PackageName}.{Version}.nupkg.{hashAlgorithm}");

                if (File.Exists(cacheHashPath) &&
                    File.ReadAllText(cacheHashPath) == Hash.Substring(hashSplitterPos + 1))
                {
                    if (TryResolvePackagePath(_nugetPackages, out packagePath))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool TryResolvePackagePath(string basePath, out string packagePath)
        {
            packagePath = Path.Combine(basePath, PackageName, Version);
            if (Directory.Exists(packagePath))
            {
                return true;
            }
            return false;
        }

        private static string GetDefaultPackageDirectory()
        {
            string basePath;
            if (PlatformServices.Default.Runtime.OperatingSystemPlatform == Platform.Windows)
            {
                basePath = Environment.GetEnvironmentVariable("USERPROFILE");
            }
            else
            {
                basePath = Environment.GetEnvironmentVariable("HOME");
            }
            if (string.IsNullOrEmpty(basePath))
            {
                return null;
            }
            return Path.Combine(basePath, ".nuget", "packages");
        }

        private static Assembly GetEntryAssembly()
        {
            var entryAssembly = (Assembly)typeof(Assembly).GetTypeInfo().GetDeclaredMethod("GetEntryAssembly").Invoke(null, null);
            if (entryAssembly == null)
            {
                throw new InvalidOperationException("Could not determine entry assembly");
            }
            return entryAssembly;
        }
    }
}