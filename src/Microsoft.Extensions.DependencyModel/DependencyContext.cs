// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.Extensions.DependencyModel
{
    public class DependencyContext
    {
        private static readonly Lazy<DependencyContext> _defaultContext = new Lazy<DependencyContext>(LoadDefault);

        public DependencyContext(string targetFramework,
            string runtime,
            bool isPortable,
            CompilationOptions compilationOptions,
            IEnumerable<CompilationLibrary> compileLibraries,
            IEnumerable<RuntimeLibrary> runtimeLibraries,
            IEnumerable<RuntimeFallbacks> runtimeGraph)
        {
            if (string.IsNullOrEmpty(targetFramework))
            {
                throw new ArgumentException(nameof(targetFramework));
            }
            if (compilationOptions == null)
            {
                throw new ArgumentNullException(nameof(compilationOptions));
            }
            if (compileLibraries == null)
            {
                throw new ArgumentNullException(nameof(compileLibraries));
            }
            if (runtimeLibraries == null)
            {
                throw new ArgumentNullException(nameof(runtimeLibraries));
            }
            if (runtimeGraph == null)
            {
                throw new ArgumentNullException(nameof(runtimeGraph));
            }

            TargetFramework = targetFramework;
            Runtime = runtime;
            IsPortable = isPortable;
            CompilationOptions = compilationOptions;
            CompileLibraries = compileLibraries.ToArray();
            RuntimeLibraries = runtimeLibraries.ToArray();
            RuntimeGraph = runtimeGraph.ToArray();
        }

        public static DependencyContext Default => _defaultContext.Value;

        public string TargetFramework { get; }

        public string Runtime { get; }

        public bool IsPortable { get; }

        public CompilationOptions CompilationOptions { get; }

        public IReadOnlyList<CompilationLibrary> CompileLibraries { get; }

        public IReadOnlyList<RuntimeLibrary> RuntimeLibraries { get; }

        public IReadOnlyList<RuntimeFallbacks> RuntimeGraph { get; }

        public DependencyContext Merge(DependencyContext other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            return new DependencyContext(
                TargetFramework,
                Runtime,
                IsPortable,
                CompilationOptions,
                CompileLibraries.Union(other.CompileLibraries, new LibraryMergeEqualityComparer<CompilationLibrary>()),
                RuntimeLibraries.Union(other.RuntimeLibraries, new LibraryMergeEqualityComparer<RuntimeLibrary>()),
                RuntimeGraph.Union(other.RuntimeGraph)
                );
        }

        public IEnumerable<RuntimeAsset> ResolveNativeAssets() => ResolveNativeAssets(PlatformServices.Default.Runtime.GetRuntimeIdentifier());

        public IEnumerable<RuntimeAsset> ResolveNativeAssets(string runtimeIdentifier) => ResolveAssets(runtimeIdentifier, a => a.NativeLibraryGroups);

        public IEnumerable<RuntimeAsset> ResolveRuntimeAssets() => ResolveRuntimeAssets(PlatformServices.Default.Runtime.GetRuntimeIdentifier());

        public IEnumerable<RuntimeAsset> ResolveRuntimeAssets(string runtimeIdentifier) => ResolveAssets(runtimeIdentifier, a => a.RuntimeAssemblyGroups);

        private static DependencyContext LoadDefault()
        {
            return DependencyContextLoader.Default.Load(Assembly.GetEntryAssembly());
        }

        public static DependencyContext Load(Assembly assembly)
        {
            return DependencyContextLoader.Default.Load(assembly);
        }

        private class LibraryMergeEqualityComparer<T> : IEqualityComparer<T> where T : Library
        {
            public bool Equals(T x, T y)
            {
                return string.Equals(x.Name, y.Name, StringComparison.Ordinal);
            }

            public int GetHashCode(T obj)
            {
                return obj.Name.GetHashCode();
            }
        }

        private IEnumerable<RuntimeAsset> ResolveAssets(string runtimeIdentifier, Func<RuntimeLibrary, IEnumerable<RuntimeAssetGroup>> groupSelector)
        {
            var fallbacks = RuntimeGraph.FirstOrDefault(f => f.Runtime == runtimeIdentifier);
            var rids = Enumerable.Concat(new[] { runtimeIdentifier }, fallbacks?.Fallbacks ?? Enumerable.Empty<string>());
            return RuntimeLibraries.SelectMany(l => SelectAssets(rids, groupSelector(l)));
        }

        private IEnumerable<RuntimeAsset> SelectAssets(IEnumerable<string> rids, IEnumerable<RuntimeAssetGroup> groups)
        {
            foreach (var rid in rids)
            {
                var group = groups.FirstOrDefault(g => g.Runtime == rid);
                if (group != null)
                {
                    return group.Assets;
                }
            }

            // Return the RID-agnostic group
            return groups.GetGroup(string.Empty);
        }
    }
}
