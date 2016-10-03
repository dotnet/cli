// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools.CrossGen;
using Microsoft.DotNet.Tools.CrossGen.Operations;
using Microsoft.Extensions.DependencyModel;

using static Microsoft.DotNet.Tools.CrossGen.Operations.FileNameConstants;

namespace Microsoft.DotNet.Tools.CrossGen.Outputs
{
    public abstract class CrossGenHandler
    {
        private class CrossGenWorkCollection
        {
            public RuntimeLibrary Lib { get; private set; }
            public ICollection<CrossGenWorkItem> WorkItems { get; private set; }

            public CrossGenWorkCollection(RuntimeLibrary lib)
            {
                Lib = lib;
                WorkItems = new List<CrossGenWorkItem>();
            }
        }

        private class CrossGenWorkItem
        {
            public string Source { get; private set; }
            public string DestinationDir { get; private set; }
            public CrossGenWorkItem(string source, string destinationDir)
            {
                Source = source;
                DestinationDir = destinationDir;
            }
        }

        // use ready2run because this is a common cache location
        // we can make this configurable if required
        private const NativeImageType DefaultCrossGenType = NativeImageType.Ready2Run;
        private readonly bool _generatePDB;
        private readonly CrossGenCmdUtil _crossGenCmds;
        private readonly IEnumerable<RuntimeLibrary> _libraries;
        private readonly IEnumerable<string> _runtimeFallbacks;
        private readonly CrossGenTarget _crossGenTarget;
        private readonly ICollection<CrossGenWorkCollection> _workItems;

        // Aggregate all the fallback directories and look them up by its runtime
        // This property could actually be a Dictionary<string, string>
        // because I am only expecting 1 fallback directory per runtime
        private readonly IDictionary<string, HashSet<string>> _fallbackDirectories;

        protected string OutputRoot { get; private set; }
        protected string AppDir { get; private set; }

        public CrossGenHandler(
            string crossGenExe,
            string diaSymReaderDll,
            CrossGenTarget crossGenTarget,
            DependencyContext depsFileContext,
            DependencyContext runtimeContext,
            string appDir,
            string outputDir,
            bool generatePDB)
        {
            AppDir = appDir;
            OutputRoot = outputDir;
            _generatePDB = generatePDB;
            _crossGenTarget = crossGenTarget;
            _workItems = new List<CrossGenWorkCollection>();
            _fallbackDirectories = new Dictionary<string, HashSet<string>>();
            _libraries = depsFileContext.RuntimeLibraries;

            var runtimeGraph = runtimeContext.RuntimeGraph.FirstOrDefault(f => f.Runtime == _crossGenTarget.RuntimeIdentifier);
            _runtimeFallbacks = runtimeGraph?.Fallbacks;

            _crossGenCmds = GetCrossGenCmds(crossGenExe, diaSymReaderDll, crossGenTarget);
        }

        public void ExecuteCrossGen()
        {
            foreach (var lib in _libraries)
            {
                PrepareCrossGen(lib);
            }

            var platformAssembliesPaths = PopulatePlatformAssembliesPaths();

            foreach (var lib in _workItems)
            {
                Reporter.Verbose.WriteLine($"CrossGen'ing {lib.Lib.Name}");
                foreach (var item in lib.WorkItems)
                {
                    Directory.CreateDirectory(item.DestinationDir);
                    _crossGenCmds.CrossGenAssembly(AppDir, item.Source, platformAssembliesPaths, item.DestinationDir, _generatePDB);
                    Reporter.Verbose.WriteLine($"Done CrossGen'd asset {item.Source}, destination directory {item.DestinationDir}");
                }
                Reporter.Verbose.WriteLine($"Finished crossGen'ing {lib.Lib.Name}");
                OnCrossGenCompletedFor(lib.Lib);
            }
            OnCrossGenCompleted();
        }

        /// <summary>
        /// Go through the library to determine what assets to crossgen and also include all the asset directories
        /// into platform assemblies path
        /// </summary>
        private void PrepareCrossGen(RuntimeLibrary lib)
        {
            if (ShouldCrossGenLib(lib))
            {
                var workItemsForLib = new CrossGenWorkCollection(lib);
                Reporter.Verbose.WriteLine($"Looking for assets to CrossGen for library {lib.Name}.{lib.Version}");
                foreach (var assetGroup in lib.RuntimeAssemblyGroups)
                {
                    var runtime = assetGroup.Runtime;

                    if (!string.IsNullOrEmpty(runtime) && runtime != _crossGenTarget.RuntimeIdentifier && !_runtimeFallbacks.Contains(runtime))
                    {
                        Reporter.Verbose.WriteLine($"Skipping assets [{string.Join(", ", assetGroup.AssetPaths)}] because targeted runtime was {runtime}");
                    }
                    else
                    {
                        foreach (var assetPath in assetGroup.AssetPaths.Where(p => p.EndsWith(".dll")))
                        {
                            var fileName = Path.GetFileName(assetPath);
                            var sourcePath = Path.Combine(AppDir, fileName);

                            if (!File.Exists(sourcePath))
                            {
                                sourcePath = Path.Combine(AppDir, assetPath);
                                if (!File.Exists(sourcePath))
                                {
                                    throw new CrossGenException($"Unable to locate asset {assetPath}");
                                }
                            }

                            // skip native
                            if (PEUtils.HasMetadata(sourcePath))
                            {
                                AddFallbackDirectory(runtime, Path.GetDirectoryName(sourcePath));
                                if (!CrossGenCmdUtil.ShouldExclude(sourcePath))
                                {
                                    var outputDir = GetOutputDirFor(sourcePath, lib, assetPath);
                                    workItemsForLib.WorkItems.Add(new CrossGenWorkItem(sourcePath, outputDir));
                                }
                            }
                        }
                    }
                }

                if (workItemsForLib.WorkItems.Count > 0)
                {
                    _workItems.Add(workItemsForLib);
                }
            }
        }

        private CrossGenCmdUtil GetCrossGenCmds(string crossGenExe, string diaSymReaderDll, CrossGenTarget crossGenTarget)
        {
            // TODO: Actually, we could support this if we really want to, we just need to copy files to to same directory
            if (_generatePDB)
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(crossGenExe);
                if (versionInfo.FileMajorPart <= 1 && versionInfo.FileMinorPart == 0)
                {
                    throw new CrossGenException($"Generate PDB is not supported for {crossGenExe}, version: {versionInfo.FileMajorPart}.{versionInfo.FileMinorPart} < 1.1.0");
                }
            }

            var jitPath = Path.Combine(AppDir, JITLibName);
            if (!File.Exists(jitPath))
            {
                jitPath = Path.Combine(crossGenTarget.SharedFrameworkDir, JITLibName);
                if (!File.Exists(jitPath))
                {
                    throw new CrossGenException($"Unable to resolve jit path. It should either be in the app directory \"{AppDir}\" or shared framework directory \"{crossGenTarget.SharedFrameworkDir}\".");
                }
            }

            string diaSymReaderPath = _generatePDB ? diaSymReaderDll : null;
            return new CrossGenCmdUtil(crossGenExe, jitPath, diaSymReaderPath, DefaultCrossGenType);
        }

        private void AddFallbackDirectory(string rid, string dir)
        {
            var fallbackKey = string.IsNullOrEmpty(rid) ? string.Empty : rid;
            HashSet<string> dirs;
            if (!_fallbackDirectories.TryGetValue(fallbackKey, out dirs))
            {
                dirs = new HashSet<string>();
                _fallbackDirectories.Add(fallbackKey, dirs);
            }
            dirs.Add(dir);
        }

        private IList<string> PopulatePlatformAssembliesPaths()
        {
            var runtimesFallbacks = _runtimeFallbacks ?? new string[] { };
            runtimesFallbacks = runtimesFallbacks.Concat(new string[] { string.Empty });

            var platformAssembliesPaths = new List<string>();

            foreach (var runtime in runtimesFallbacks)
            {
                if (_fallbackDirectories.ContainsKey(runtime))
                {
                    var dirs = _fallbackDirectories[runtime];
                    platformAssembliesPaths.AddRange(dirs);
                }
            }

            if (_crossGenTarget.SharedFrameworkDir != null)
            {
                platformAssembliesPaths.Add(_crossGenTarget.SharedFrameworkDir);
            }

            Reporter.Verbose.WriteLine($"App closure: [{string.Join(", ", platformAssembliesPaths)}]");
            return platformAssembliesPaths;
        }

        private string GetFallbackDirForManaged(string rid)
        {
            return Path.Combine(AppDir, "runtimes", rid, "lib");
        }

        protected virtual bool ShouldCrossGenLib(RuntimeLibrary lib) { return true; }
        protected virtual void OnCrossGenCompletedFor(RuntimeLibrary lib) { }
        protected virtual void OnCrossGenCompleted() { }
        protected abstract string GetOutputDirFor(string sourcePathUsed, RuntimeLibrary lib, string assetPath);

    }
}
