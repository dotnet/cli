// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli.Utils;

namespace Microsoft.DotNet.Tools.CrossGen.Operations
{
    /// <summary>
    /// This is a wrapper of crossgen.exe native image generation utiltites
    /// </summary>
    public class CrossGenCmdUtil
    {
        private enum OutputType
        {
            dll, pdb
        }

        private static readonly string[] s_excludedLibraries =
        {
            "mscorlib.dll",
            "mscorlib.ni.dll",
            "System.Private.CoreLib.dll",
            "System.Private.CoreLib.ni.dll"
        };

        private readonly string _jitPath;
        private readonly string _crossgenPath;
        private readonly string _diaSymReaderPath;
        private readonly NativeImageType? _outputType;

        public CrossGenCmdUtil(
            string crossgenPath,
            string jitPath,
            string diaSymReaderPath,
            NativeImageType? outputType = null)
        {
            _crossgenPath = crossgenPath;
            _jitPath = jitPath;
            _diaSymReaderPath = diaSymReaderPath;
            _outputType = outputType;
        }

        public static bool ShouldExclude(string assemblyLocation)
        {
            var fileName = Path.GetFileName(assemblyLocation);
            var exclude = s_excludedLibraries.Any(lib => string.Equals(lib, fileName, StringComparison.OrdinalIgnoreCase));
            if (exclude)
            {
                Reporter.Verbose.WriteLine($"Excluding {assemblyLocation} for crossgen.");
            }
            return exclude;
        }

        public ICollection<string> CrossGenAssembly(string appPath, string assemblyLocation, IList<string> platformAssembliesPaths, string outputDirectory, bool generateSymbols)
        {
            Reporter.Verbose.WriteLine($"CrossGen'ing {assemblyLocation}");
            var outputFiles = new List<string>();

            var fileName = Path.GetFileName(assemblyLocation);
            var module = fileName.Substring(0, fileName.Length - 4);
            var crossgenArgs = string.Join(" ", GetArgs(appPath, assemblyLocation, module, platformAssembliesPaths, outputDirectory, false));
            var cmd = Command.Create(new CommandSpec(_crossgenPath, crossgenArgs, CommandResolutionStrategy.None))
                .WorkingDirectory(appPath)
                // disable partial ngen
                .EnvironmentVariable("COMPlus_PartialNGen", "0")
                .CaptureStdOut()
                .CaptureStdErr();

            var result = cmd.Execute();

            var niModuleLocation = GetOutputLocation(outputDirectory, module, OutputType.dll);
            Reporter.Verbose.WriteLine(result.StdOut);
            HandleCrossGenExeStdError(result.StdErr);
            if (result.ExitCode != 0 || !File.Exists(niModuleLocation))
            {
                throw new CrossGenException($"Crossgen module {module} failed. Error code: {result.ExitCode}. Expected output location: {niModuleLocation}.");
            }

            outputFiles.Add(niModuleLocation);

            if (generateSymbols)
            {
                var pdbArgs = string.Join(" ", GetArgs(appPath, assemblyLocation, module, platformAssembliesPaths, outputDirectory, true));
                var pdbCmd = Command.Create(new CommandSpec(_crossgenPath, pdbArgs, CommandResolutionStrategy.None))
                                .WorkingDirectory(appPath)
                                .CaptureStdOut()
                                .CaptureStdErr();

                var pdbResult = pdbCmd.Execute();

                var pdbLocation = GetOutputLocation(outputDirectory, module, OutputType.pdb);
                Reporter.Verbose.WriteLine(result.StdOut);
                HandleCrossGenExeStdError(result.StdErr);
                if (pdbResult.ExitCode != 0 || !File.Exists(pdbLocation))
                {
                    throw new CrossGenException($"Symbol generation for module {module} failed. Error code: {pdbResult.ExitCode}. Expected output location: {pdbLocation}.");
                }

                outputFiles.Add(pdbLocation);
            }

            Reporter.Verbose.WriteLine($"Completed crossGen'ing {assemblyLocation}");
            return outputFiles;
        }

        private List<string> GetArgs(string appPath, string assemblyLocation, string module, IList<string> platformAssembliesPaths, string outputDirectory, bool isSymbolGeneration)
        {
            var parameters = new List<string>();

            if (platformAssembliesPaths != null && platformAssembliesPaths.Count > 0)
            {
                parameters.Add("-platform_assemblies_paths");
                parameters.Add(string.Join(";", platformAssembliesPaths.Select(path => $"\"{path}\"")));
            }

            if (!string.IsNullOrEmpty(appPath))
            {
                parameters.Add("-App_Paths");
                parameters.Add($"\"{appPath}\"");
            }

            parameters.Add("-JITPath");
            parameters.Add(_jitPath);

            var outputDllLocation = GetOutputLocation(outputDirectory, module, OutputType.dll);
            if (isSymbolGeneration)
            {
                parameters.Add("-CreatePDB");
                parameters.Add(GetOutputLocation(outputDirectory, module, OutputType.pdb));

                parameters.Add("-DiasymreaderPath");
                parameters.Add(_diaSymReaderPath);

                parameters.Add(outputDllLocation);
            }
            else
            {
                if (_outputType.HasValue)
                {
                    switch (_outputType)
                    {
                        case NativeImageType.FragileNonVersionable:
                            parameters.Add("-FragileNonVersionable");
                            break;
                        case NativeImageType.Ready2Run:
                            parameters.Add("-readytorun");
                            break;
                    }
                }

                parameters.Add("-out");
                parameters.Add(outputDllLocation);

                parameters.Add(assemblyLocation);
            }

            return parameters;
        }

        private void HandleCrossGenExeStdError(string erroString)
        {
            // crossgen.exe would return a single \n in stdError even if it passes
            var actualError = erroString.Trim();
            if (actualError.Length > 0)
            {
                Reporter.Error.WriteLine(actualError);
            }
        }

        private string GetOutputLocation(string outputDirectory, string module, OutputType type)
        {
            return Path.Combine(outputDirectory, $"{module}.{type}");
        }
    }
}
