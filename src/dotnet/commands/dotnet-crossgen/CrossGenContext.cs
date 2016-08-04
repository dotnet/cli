// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.PlatformAbstractions;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.Tools.CrossGen.Outputs;
using Microsoft.Extensions.DependencyModel;
using NuGet.Frameworks;
using NuGet.Versioning;

using static Microsoft.DotNet.Tools.CrossGen.Operations.FileNameConstants;

namespace Microsoft.DotNet.Tools.CrossGen
{
    public class CrossGenContext
    {
        private readonly string _appName;
        private readonly string _appDir;
        private readonly bool _generatePDB;
        private CrossGenTarget _crossGenTarget;

        /// <summary>
        /// Context taken straight out of the app's deps file,
        /// This context determine what assets to crossgen
        /// </summary>
        private DependencyContext _depsFileContext;

        /// <summary>
        /// Context that is merged with the shared framework (if applicable)
        /// This context is the actual context used on run time and is
        /// used to determine runtime fallbacks
        /// </summary>
        private DependencyContext _runtimeContext;
        
        public CrossGenContext(string appName, string appDir, bool generatePDB)
        {
            _appName = appName;
            _appDir = appDir;
            _generatePDB = generatePDB;
        }

        public void Initialize()
        {
            var depsFilePath = Path.Combine(_appDir, $"{_appName}.deps.json");
            if (!File.Exists(depsFilePath))
            {
                throw new CrossGenException($"Deps {depsFilePath} file not found");
            }

            string sharedFrameworkDir;
            NuGetFramework framework;
            string rid;
            using (var reader = new DependencyContextJsonReader())
            {
                using (var fstream = new FileStream(depsFilePath, FileMode.Open))
                {
                    _depsFileContext = reader.Read(fstream);
                }

                if (_depsFileContext == null)
                {
                    throw new CrossGenException($"Unexpected error while reading {depsFilePath}");
                }

                var runtimeConfigPath = Path.Combine(_appDir, $"{_appName}.runtimeconfig.json");
                RuntimeConfig runtimeConfig = null;
                if (File.Exists(runtimeConfigPath))
                {
                    runtimeConfig = new RuntimeConfig(runtimeConfigPath);
                }

                if (runtimeConfig != null && runtimeConfig.IsPortable)
                {
                    // This is portable app
                    Reporter.Verbose.WriteLine($"This is a portable app, runtime config file: {runtimeConfigPath}");
                    sharedFrameworkDir = LocateSharedFramework(runtimeConfig.Framework);
                    var shreadFrameworkDepsFile = Path.Combine(sharedFrameworkDir, $"{runtimeConfig.Framework.Name}.deps.json");
                    if (!File.Exists(shreadFrameworkDepsFile))
                    {
                        throw new CrossGenException($"Cannot locate share framework's deps file {shreadFrameworkDepsFile}");
                    }

                    using (var fstream = new FileStream(shreadFrameworkDepsFile, FileMode.Open))
                    {
                        _runtimeContext = reader.Read(fstream);
                    }

                    // After merging, framework and rid would be gone. So do this pre-merge.
                    framework = NuGetFramework.Parse(_runtimeContext.Target.Framework);
                    rid = _runtimeContext.Target.Runtime;

                    if (_runtimeContext == null)
                    {
                        throw new CrossGenException($"Unable to load shared framework context from {shreadFrameworkDepsFile}");
                    }

                    _runtimeContext = _depsFileContext.Merge(_runtimeContext);
                }
                else
                {
                    Reporter.Verbose.WriteLine($"This is a standalone app, runtime config file: {(runtimeConfig == null ? runtimeConfigPath : "None")}");
                    sharedFrameworkDir = null;
                    _runtimeContext = _depsFileContext;
                    framework = NuGetFramework.Parse(_runtimeContext.Target.Framework);
                    rid = _runtimeContext.Target.Runtime;
                }
            }

            if (framework.Framework != ".NETCoreApp")
            {
                throw new CrossGenException($"App targets {_crossGenTarget.Framework.Framework} cannot be CrossGen'd, supported frameworks: [.NETCoreApp].");
            }

            _crossGenTarget = new CrossGenTarget(framework, rid, sharedFrameworkDir);

            Reporter.Verbose.WriteLine($"CrossGen will be performed to target Framework: {_crossGenTarget.Framework}, RID: {_crossGenTarget.RuntimeIdentifier}");
        }

        public void ExecuteCrossGen(string crossGenExe, string diaSymReaderDll, string outputDir, CrossGenOutputStructure structure, bool overwriteHash)
        {
            if (_generatePDB && diaSymReaderDll == null)
            {
                diaSymReaderDll = FindDiaSymReader();
            }

            CrossGenHandler crossGenHandler;
            switch (structure)
            {
                case CrossGenOutputStructure.APP:
                    crossGenHandler = new AppCrossGenHandler(crossGenExe, diaSymReaderDll, _crossGenTarget, _depsFileContext, _runtimeContext, _appDir, outputDir, _generatePDB);
                    break;
                
                case CrossGenOutputStructure.CACHE:
                    crossGenHandler = new OptimizationCacheCrossGenHandler(crossGenExe, diaSymReaderDll, _crossGenTarget, _depsFileContext, _runtimeContext, _appDir, outputDir, _generatePDB, overwriteHash);
                    break;

                default:
                    throw new CrossGenException($"Invalid output structure: {structure}");
            }

            crossGenHandler.ExecuteCrossGen();
        }

        private string FindDiaSymReader()
        {
            var targetRid = _crossGenTarget.RuntimeIdentifier;
            var hostDepContext = DependencyContext.Default;
            var ridFallback = hostDepContext.RuntimeGraph.FirstOrDefault(fallback => fallback.Runtime == _crossGenTarget.RuntimeIdentifier);
            IEnumerable<string> ridList = new string[] { targetRid };
            if (ridFallback == null)
            {
                Reporter.Verbose.WriteLine($"Runtime {targetRid} fallback is not defined.");
                ridList = new string[] { targetRid };
            }
            else
            {
                ridList = ridList.Concat(ridFallback.Fallbacks);
            }

            var arch = RuntimeEnvironment.RuntimeArchitecture;
            var diaSymReaderFileName = $"{DynamicLibPrefix}Microsoft.DiaSymReader.Native.{arch}{DynamicLibSuffix}";
            var probeLocations = new string[] { Path.Combine(ApplicationEnvironment.ApplicationBasePath, diaSymReaderFileName) }.Concat(
                    ridList.Select(rid => Path.Combine(ApplicationEnvironment.ApplicationBasePath, "runtimes", rid, "native", diaSymReaderFileName)));

            // x64, aka amd64
            if (arch == "x64")
            {
                var archSuffix = $".{arch}{DynamicLibSuffix}";
                probeLocations = probeLocations.Concat(
                                    probeLocations.Where(l => l.EndsWith(archSuffix))
                                                    .Select(l => l.Substring(0, l.Length - archSuffix.Length) + $".amd64{DynamicLibSuffix}"));
            }

            var foundLocation = probeLocations.FirstOrDefault(l => File.Exists(l));

            if (foundLocation == null)
            {
                throw new CrossGenException($"Failed to locate DiaSymReader for runtime {targetRid}");
            }

            Reporter.Verbose.WriteLine($"Found DiaSymReader {foundLocation}");
            return foundLocation;
        }

        private string LocateSharedFramework(RuntimeConfigFramework framework)
        {
            var dotnetHome = Path.GetDirectoryName(new Muxer().MuxerPath);
            var shareFrameworksDir = Path.Combine(dotnetHome, "shared", framework.Name);

            if (!Directory.Exists(shareFrameworksDir))
            {
                throw new CrossGenException($"Shared framework {shareFrameworksDir} does not exist");
            }

            var version = framework.Version;
            var exactMatch = Path.Combine(shareFrameworksDir, version);
            if (Directory.Exists(exactMatch))
            {
                return exactMatch;
            }
            else
            {
                Reporter.Verbose.WriteLine($"Cannot find shared framework in: {exactMatch}, trying to auto roll forward.");
                return AutoRollForward(shareFrameworksDir, version);
            }
        }

        private string AutoRollForward(string root, string version)
        {
            var targetVersion = NuGetVersion.Parse(version);
            var candidateNames = Directory.GetDirectories(root).Select(d => Path.GetFileName(d));

            string bestMatch = null;
            NuGetVersion bestMatchVersion = null;
            foreach (var candidateName in candidateNames)
            {
                var currentVersion = NuGetVersion.Parse(candidateName);
                if (currentVersion.Major == targetVersion.Major &&
                    currentVersion.Minor == targetVersion.Minor &&
                    currentVersion.CompareTo(targetVersion) > 0)
                {
                    if (bestMatchVersion == null || currentVersion.CompareTo(bestMatchVersion) < 0)
                    {
                        bestMatchVersion = currentVersion;
                        bestMatch = candidateName;
                    }
                }
            }

            if (bestMatch == null)
            {
                throw new CrossGenException($"Unable to find shared framework candidate in {root}. Base version: {version}, available [{string.Join(", ", candidateNames)}]");
            }

            Reporter.Output.WriteLine($"Shared framework {bestMatch} would be used to crossgen.");

            return Path.Combine(root, bestMatch);
        }
    }
}
