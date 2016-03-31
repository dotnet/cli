using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.DotNet.Cli.Utils;

namespace Microsoft.DotNet.Tools.Compiler.Native
{
    /// <summary>
    /// Compiles the System.Private.* libraries into a single linkable object file
    /// </summary>
    internal class IlcSdkCompilationStep : ILCompilationStep
    {
        internal static readonly string CoreLibrary = "System.Private.CoreLib.dll";

        public IlcSdkCompilationStep(NativeCompileSettings config) : base(config)
        {
            Debug.Assert(config.IsMultiModuleBuild);
        }

        protected override void InitializeArgs(NativeCompileSettings config)
        {
            var argsList = new List<string>();

            // Input File
            // TODO:    Compile all System.Private.* assemblies into SDK.obj
            var coreLibsPath = Path.Combine(config.IlcSdkPath, "sdk");
            argsList.Add(Path.Combine(coreLibsPath, CoreLibrary));
            argsList.Add("--multifile");
            argsList.Add($"-o:{DetermineOutputFile(config)}");

            Args = argsList;
        }

        public override bool OutputIsUpToDate
        {
            get
            {
                string outFile = DetermineOutputFile(config);
                var inputFile = Path.Combine(Path.Combine(config.IlcSdkPath, "sdk"), CoreLibrary);
                var ilcExePath = Path.Combine(config.IlcPath, ILCompiler);

                if (!File.Exists(outFile))
                    return false;

                if (!File.Exists(inputFile))
                    return false;

                var outFileCreationTime = File.GetCreationTimeUtc(outFile);
                var inputFileCreationTime = File.GetCreationTimeUtc(inputFile);
                if (inputFileCreationTime > outFileCreationTime)
                    return false;

                if (!File.Exists(ilcExePath))
                    return false;

                var ilcExeCreationTime = File.GetCreationTimeUtc(ilcExePath);
                if (ilcExeCreationTime > outFileCreationTime)
                    return false;

                return true;
            }
        }

        private string DetermineOutputFile(NativeCompileSettings config)
        {
            var extension = ModeOutputExtensionMap[config.NativeMode];
            var outFile = Path.Combine(config.IlcSdkPath, "sdk", "sdk" + extension);
            return outFile;
        }
    }
}
