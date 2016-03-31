using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.DotNet.Cli.Utils;

namespace Microsoft.DotNet.Tools.Compiler.Native
{
    internal abstract class ILCompilationStep
    {
        protected static readonly string HostExeName = "corerun" + Constants.ExeSuffix;
        protected static readonly string ILCompiler = "ilc.exe";

        protected IEnumerable<string> Args;
        protected NativeCompileSettings config;

        protected static readonly Dictionary<NativeIntermediateMode, string> ModeOutputExtensionMap = new Dictionary<NativeIntermediateMode, string>
        {
            { NativeIntermediateMode.cpp, ".cpp" },
            { NativeIntermediateMode.ryujit, ".obj" }
        };

        protected ILCompilationStep(NativeCompileSettings config)
        {
            this.config = config;
            InitializeArgs(config);
        }

        protected abstract void InitializeArgs(NativeCompileSettings config);
        public abstract bool OutputIsUpToDate { get; }

        public int Invoke()
        {
            // Check if ILCompiler is present
            var ilcExePath = Path.Combine(config.IlcPath, ILCompiler);
            if (!File.Exists(ilcExePath))
            {
                throw new FileNotFoundException("Unable to find ILCompiler at " + ilcExePath);
            }

            // Write the response file
            var intermediateDirectory = config.IntermediateDirectory;
            var rsp = Path.Combine(intermediateDirectory, "dotnet-compile-native-ilc.rsp");
            File.WriteAllLines(rsp, Args, Encoding.UTF8);

            var hostPath = Path.Combine(config.IlcPath, HostExeName);
            var result = Command.Create(hostPath, new string[] { ilcExePath, "@" + $"{rsp}" })
                .ForwardStdErr()
                .ForwardStdOut()
                .Execute();

            return result.ExitCode;
        }
    }
}
