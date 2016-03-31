using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.DotNet.Cli.Utils;

namespace Microsoft.DotNet.Tools.Compiler.Native
{
    /// <summary>
    /// Compiles the application and its binaries into a single linkable object file
    /// </summary>
    internal class AppCompilationStep : ILCompilationStep
    {
        public AppCompilationStep(NativeCompileSettings config) : base(config) { }

        protected override void InitializeArgs(NativeCompileSettings config)
        {
            var argsList = new List<string>();

            // Input File 
            var inputFilePath = config.InputManagedAssemblyPath;
            argsList.Add($"{inputFilePath}");

            // System.Private.* References
            var coreLibsPath = Path.Combine(config.IlcSdkPath, "sdk");
            foreach (var reference in Directory.EnumerateFiles(coreLibsPath, "*.dll"))
            {
                if (config.IsMultiModuleBuild && !reference.EndsWith(IlcSdkCompilationStep.CoreLibrary, StringComparison.OrdinalIgnoreCase))
                {
                    argsList.Add(reference);
                }
                else
                {
                    argsList.Add($"-r:{reference}");
                }
            }

            // AppDep References
            foreach (var reference in config.ReferencePaths)
            {
                if (config.IsMultiModuleBuild)
                {
                    argsList.Add(reference);
                }
                else
                {
                    argsList.Add($"-r:{reference}");
                }
            }

            // Set Output DetermineOutFile
            var outFile = DetermineOutputFile(config);
            argsList.Add($"-o:{outFile}");

            // Add Mode Flag TODO
            if (config.NativeMode == NativeIntermediateMode.cpp)
            {
                argsList.Add("--cpp");
            }

            if (config.IsMultiModuleBuild)
            {
                argsList.Add("--multifile");
            }

            // Custom Ilc Args support
            foreach (var ilcArg in config.IlcArgs)
            {
                argsList.Add(ilcArg);
            }

            Args = argsList;
        }

        public override bool OutputIsUpToDate
        {
            get
            {
                return false;
            }
        }

        private string DetermineOutputFile(NativeCompileSettings config)
        {
            var intermediateDirectory = config.IntermediateDirectory;

            var extension = ModeOutputExtensionMap[config.NativeMode];

            var filename = Path.GetFileNameWithoutExtension(config.InputManagedAssemblyPath);

            var outFile = Path.Combine(intermediateDirectory, filename + extension);

            return outFile;
        }

    }
}
