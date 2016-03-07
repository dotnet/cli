﻿using System.Collections.Generic;
using System.IO;
using Microsoft.DotNet.Cli.Utils;
using System.Linq;

namespace Microsoft.DotNet.Tools.Compiler.Native
{
    public class LinuxRyuJitCompileStep : IPlatformNativeStep
    {
        private const string CompilerName = "clang-3.5";
        private const string InputExtension = ".obj";
        private const string CompilerOutputExtension = "";

        public List<string> CompilerArgs { get; set; }

        // TODO: debug/release support
        private readonly string[] _cflags = {"-lstdc++", "-lpthread", "-ldl", "-lm", "-lrt"};
        private readonly string[] _ilcSdkLibs = 
            {
                "libbootstrapper.a",
                "libRuntime.a",
                "libSystem.Private.CoreLib.Native.a"
            };

        private readonly string[] _appdeplibs = 
            {
                "System.Native.a"
            };

        public LinuxRyuJitCompileStep(NativeCompileSettings config)
        {
            InitializeArgs(config);
        }

        public int Invoke()
        {
            var result = InvokeCompiler();
            if (result != 0)
            {
                Reporter.Error.WriteLine("Compilation of intermediate files failed.");
            }

            return result;
        }

        public bool CheckPreReqs()
        {
            // TODO check for clang
            return true;
        }

        private void InitializeArgs(NativeCompileSettings config)
        {
            var argsList = new List<string>();

            // Flags
            argsList.AddRange(_cflags);
            
            // Input File
            var inLibFile = DetermineInFile(config);
            argsList.Add(inLibFile);

            // Pass the optional native compiler flags if specified
            if (!string.IsNullOrWhiteSpace(config.CppCompilerFlags))
            {
                argsList.Add(config.CppCompilerFlags);
            }
            
            // ILC SDK Libs
            var ilcSdkLibPath = Path.Combine(config.IlcSdkPath, "sdk");
            argsList.AddRange(_ilcSdkLibs.Select(lib => Path.Combine(ilcSdkLibPath, lib)));

            // AppDep Libs
            var baseAppDepLibPath = Path.Combine(config.AppDepSDKPath, "CPPSdk/ubuntu.14.04", config.Architecture.ToString());
            argsList.AddRange(_appdeplibs.Select(lib => Path.Combine(baseAppDepLibPath, lib)));

            // Output
            var libOut = DetermineOutputFile(config);
            argsList.Add($"-o");
            argsList.Add($"{libOut}");

            CompilerArgs = argsList;
        }

        private int InvokeCompiler()
        {
            var result = Command.Create(CompilerName, CompilerArgs)
                .ForwardStdErr()
                .ForwardStdOut()
                .Execute();

            return result.ExitCode;
        }

        private string DetermineInFile(NativeCompileSettings config)
        {
            var intermediateDirectory = config.IntermediateDirectory;

            var filename = Path.GetFileNameWithoutExtension(config.InputManagedAssemblyPath);

            var infile = Path.Combine(intermediateDirectory, filename + InputExtension);

            return infile;
        }

        public string DetermineOutputFile(NativeCompileSettings config)
        {
            var intermediateDirectory = config.OutputDirectory;

            var filename = Path.GetFileNameWithoutExtension(config.InputManagedAssemblyPath);

            var outfile = Path.Combine(intermediateDirectory, filename + CompilerOutputExtension);

            return outfile;
        }
    }
}
