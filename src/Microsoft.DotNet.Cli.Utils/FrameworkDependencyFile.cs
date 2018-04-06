// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.PlatformAbstractions;

using Microsoft.Extensions.DependencyModel;

namespace Microsoft.DotNet.Cli.Utils
{
    /// <summary>
    /// Represents the .deps.json file in the shared framework
    /// that the CLI is running against.
    /// </summary>
    internal class FrameworkDependencyFile
    {
        private readonly string _depsFilePath;
        private readonly Lazy<DependencyContext> _dependencyContext;

        private DependencyContext DependencyContext => _dependencyContext.Value;

        public FrameworkDependencyFile()
        {
            _depsFilePath = Muxer.GetDataFromAppDomain("FX_DEPS_FILE");
            _dependencyContext = new Lazy<DependencyContext>(CreateDependencyContext);
        }

        public bool SupportsCurrentRuntime()
        {
            return IsRuntimeSupported(RuntimeEnvironment.GetRuntimeIdentifier());
        }

        public bool IsRuntimeSupported(string runtimeIdentifier)
        {
            return DependencyContext.RuntimeGraph.Any(g => g.Runtime == runtimeIdentifier);
        }

        public string GetNetStandardLibraryVersion()
        {
            return DependencyContext
                .RuntimeLibraries
                .FirstOrDefault(l => "netstandard.library".Equals(l.Name, StringComparison.OrdinalIgnoreCase))
                ?.Version;
        }

        public bool TryGetMostFitRuntimeIdentifier(
            IEnumerable<string> candidateRuntimeIdentifiers,
            out string mostFitRuntimeIdentifier,
            string alternative = null)
        {
            mostFitRuntimeIdentifier = null;
            var runtimeIdentifier = RuntimeEnvironment.GetRuntimeIdentifier();
            var runtimeFallbacksCandidates =
                DependencyContext.RuntimeGraph
                    .Where(g => string.Equals(g.Runtime, runtimeIdentifier, StringComparison.OrdinalIgnoreCase))
                    .ToArray();

            if (runtimeFallbacksCandidates.Length == 0 || !string.IsNullOrEmpty(alternative))
            {
                runtimeFallbacksCandidates =
                    DependencyContext.RuntimeGraph
                        .Where(g => string.Equals(alternative, runtimeIdentifier, StringComparison.OrdinalIgnoreCase))
                        .ToArray();
            }

            if (runtimeFallbacksCandidates.Length == 0)
            {
                return false;
            }

            RuntimeFallbacks runtimeFallbacks = runtimeFallbacksCandidates[0];

            var candidateRuntimeIdentifiersArray = candidateRuntimeIdentifiers.ToArray();

            var runtimeFallbacksIncludesRuntime =
                runtimeFallbacks.Fallbacks.ToList();
            runtimeFallbacksIncludesRuntime.Insert(0, runtimeFallbacks.Runtime);

            foreach (var fallbackruntime in runtimeFallbacksIncludesRuntime)
            {
                var index = Array.FindIndex(
                    candidateRuntimeIdentifiersArray,
                    c => string.Equals(c, fallbackruntime, StringComparison.OrdinalIgnoreCase));

                if (index >= 0)
                {
                    mostFitRuntimeIdentifier = candidateRuntimeIdentifiersArray[index];
                    return true;
                }
            }

            return false;
        }

        private DependencyContext CreateDependencyContext()
        {
            using (Stream depsFileStream = File.OpenRead(_depsFilePath))
            using (DependencyContextJsonReader reader = new DependencyContextJsonReader())
            {
                return reader.Read(depsFileStream);
            }
        }
    }
}
