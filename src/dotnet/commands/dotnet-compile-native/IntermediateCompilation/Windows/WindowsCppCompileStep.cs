using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Microsoft.Dnx.Runtime.Common.CommandLine;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools.Common;

namespace Microsoft.DotNet.Tools.Compiler.Native
{
    public class WindowsCppCompileStep : IPlatformNativeStep
    {
        //TODO support x86
        private readonly string CompilerName = "cl.exe";
        
        private readonly string VSBin = "..\\..\\VC\\bin\\amd64";
        private readonly string InputExtension = ".cpp";
        
        private readonly string CompilerOutputExtension = ".obj";

        private static readonly string[] DefaultCompilerOptions = { "/nologo", "/W3", "/GS", "/DCPPCODEGEN", "/EHs", "/MT", "/Zi" };

        private static readonly Dictionary<BuildConfiguration, string[]> ConfigurationCompilerOptionsMap = new Dictionary<BuildConfiguration, string[]>
        {
            { BuildConfiguration.debug, new string[] { "/Od" } },
            { BuildConfiguration.release, new string[] { "/O2" } }
        };
        
        private IEnumerable<string> CompilerArgs { get; set; }

        private NativeCompileSettings config;
        
        public WindowsCppCompileStep(NativeCompileSettings config)
        {
            this.config = config;
            InitializeArgs(config);
        }
        
        public int Invoke()
        {
            var result = WindowsCommon.SetVCVars();
            if (result != 0)
            {
                Reporter.Error.WriteLine("vcvarsall.bat invocation failed.");
                return result;
            }
            
            result = InvokeCompiler();
            if (result != 0)
            {
                Reporter.Error.WriteLine("Compilation of intermediate files failed.");
            }
            
            return result;
        }
        
        public bool CheckPreReqs()
        {
            var vcInstallDir = Environment.GetEnvironmentVariable("VS140COMNTOOLS");
            return !string.IsNullOrEmpty(vcInstallDir);
        }
        
        private void InitializeArgs(NativeCompileSettings config)
        {
            var argsList = new List<string>();
            
            // Use a Custom Link Step
            argsList.Add("/c");
            
            // Add Includes
            var ilcSdkIncPath = Path.Combine(config.IlcSdkPath, "inc");
            argsList.Add("/I");
            argsList.Add($"{ilcSdkIncPath}");
            
            argsList.AddRange(DefaultCompilerOptions);
            
            // Configuration Based Compiler Options 
            argsList.AddRange(ConfigurationCompilerOptionsMap[config.BuildType]);
            
            // Pass the optional native compiler flags if specified
            if (!string.IsNullOrWhiteSpace(config.CppCompilerFlags))
            {
                argsList.Add(config.CppCompilerFlags);
            }

            // Output
            var objOut = DetermineOutputFile(config);
            argsList.Add($"/Fo{objOut}");
            
            // Input File
            var inCppFile = DetermineInFile(config);
            argsList.Add($"{inCppFile}");

            this.CompilerArgs = argsList;
        }
        
        private int InvokeCompiler()
        {
            var vcInstallDir = Environment.GetEnvironmentVariable("VS140COMNTOOLS");
            var compilerPath = Path.Combine(vcInstallDir, VSBin, CompilerName);
            
            var result = Command.Create(compilerPath, CompilerArgs.ToArray())
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
            var intermediateDirectory = config.IntermediateDirectory;

            var filename = Path.GetFileNameWithoutExtension(config.InputManagedAssemblyPath);

            var outfile = Path.Combine(intermediateDirectory, filename + CompilerOutputExtension);
            
            return outfile;
        }
        
    }
}
