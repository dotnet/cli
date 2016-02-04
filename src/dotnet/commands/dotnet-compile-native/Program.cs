using System;
using System.IO;
using Microsoft.DotNet.Cli.Utils;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Tools.Compiler.Native
{
    public class CompileNativeCommand
    {
        private const string McgInteropAssemblySuffix = ".mcginterop.dll";
        public static int Run(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);

            return ExecuteApp(args);
        }        

        private static int ExecuteApp(string[] args)
        {   
            // Support Response File
            foreach(var arg in args)
            {
                if(arg.Contains(".rsp"))
                {
                    args = ParseResponseFile(arg);

                    if (args == null)
                    {
                        return 1;
                    }
                }
            }

            try
            {
                var cmdLineArgs = ArgumentsParser.Parse(args);

                if (cmdLineArgs.IsHelp) return cmdLineArgs.ReturnCode;

                var config = cmdLineArgs.GetNativeCompileSettings();

                DirectoryExtensions.CleanOrCreateDirectory(config.OutputDirectory);
                DirectoryExtensions.CleanOrCreateDirectory(config.IntermediateDirectory);

                // run mcg if requested
                if (config.EnableInterop)
                {
                    int exitCode = 0;
                    if ((exitCode = RunMcg(config)) > 0)
                    {
                        return exitCode;
                    }
                    CompileMcgGeneratedCode(config);
                }

                var nativeCompiler = NativeCompiler.Create(config);

                var result = nativeCompiler.CompileToNative(config);

                return result ? 0 : 1;
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex);
#else
                Reporter.Error.WriteLine(ex.Message);
#endif
                return 1;
            }
        }

        private static int RunMcg(NativeCompileSettings config)
        {
            var mcgArgs = new List<string>();
            string outPath = Path.Combine(Path.GetDirectoryName(config.InputManagedAssemblyPath), "Interop");
            mcgArgs.Add($"{config.InputManagedAssemblyPath}");
            mcgArgs.Add("--p");
            mcgArgs.Add(config.Architecture.ToString());
            mcgArgs.Add("--outputpath");
            mcgArgs.Add(outPath);

            var ilSdkPath = Path.Combine(config.IlcSdkPath, "sdk");        
                
            mcgArgs.Add("--r");
            mcgArgs.Add(Path.Combine(ilSdkPath,"System.Private.Interop.dll"));
            mcgArgs.Add("--r");
            mcgArgs.Add(Path.Combine(ilSdkPath, "System.Private.CoreLib.dll"));
            mcgArgs.Add("--r");
            mcgArgs.Add(Path.Combine(config.AppDepSDKPath, "System.Runtime.Handles.dll"));
            mcgArgs.Add("--r");
            mcgArgs.Add(Path.Combine(config.AppDepSDKPath, "System.Runtime.dll"));


            // Write Response File
            var rsp = Path.Combine(config.IntermediateDirectory, $"dotnet-compile-mcg.rsp");
            File.WriteAllLines(rsp, mcgArgs);

            var corerun = Path.Combine(AppContext.BaseDirectory, Constants.HostExecutableName);
            var mcgExe = Path.Combine(AppContext.BaseDirectory, "mcg.exe");

            List<string> args = new List<string>();
            args.Add(mcgExe);
            args.AddRange(new string[] { "--rsp", $"{rsp}" });


            var result = Command.Create(corerun, args.ToArray())
                                .ForwardStdErr()
                                .ForwardStdOut()
                                .Execute();
            
            return result.ExitCode;
        }

        private static int CompileMcgGeneratedCode(NativeCompileSettings config)
        {
            List<string> cscArgs = new List<string>();
            string mcgOutPath = Path.Combine(Path.GetDirectoryName(config.InputManagedAssemblyPath), "Interop");
            cscArgs.AddRange(Directory.EnumerateFiles(mcgOutPath, "*.cs"));

            var ilSdkPath = Path.Combine(config.IlcSdkPath, "sdk");
            cscArgs.AddRange(Directory.EnumerateFiles(ilSdkPath, "*.dll").Select(r => $"--reference:{r}"));

            cscArgs.AddRange(Directory.EnumerateFiles(config.AppDepSDKPath, "*.dll").Select(r => $"--reference:{r}"));

            cscArgs.Add("--reference");
            cscArgs.Add(config.InputManagedAssemblyPath);


            var interopAssemblyPath = Path.Combine(mcgOutPath, Path.GetFileNameWithoutExtension(config.InputManagedAssemblyPath));
            interopAssemblyPath += McgInteropAssemblySuffix;

            cscArgs.Add("--out");
            cscArgs.Add(interopAssemblyPath);

            cscArgs.Add("--temp-output");
            cscArgs.Add(config.IntermediateDirectory);

            cscArgs.Add("--define");
            cscArgs.Add("DEBUG");

            cscArgs.Add("--emit-entry-point");
            cscArgs.Add("False");

            cscArgs.Add("--file-version");
            cscArgs.Add("1.0.0.0");

            cscArgs.Add("--version");
            cscArgs.Add("1.0.0.0");

            config.AddReference(interopAssemblyPath);
            return Microsoft.DotNet.Tools.Compiler.Csc.CompileCscCommand.Run(cscArgs.ToArray());
        }

        private static string[] ParseResponseFile(string rspPath)
        {
            if (!File.Exists(rspPath))
            {
                Reporter.Error.WriteLine("Invalid Response File Path");
                return null;
            }

            string content;
            try
            {
                content = File.ReadAllText(rspPath);
            }
            catch (Exception)
            {
                Reporter.Error.WriteLine("Unable to Read Response File");
                return null;
            }

            var nArgs = content.Split(new [] {"\r\n", "\n"}, StringSplitOptions.RemoveEmptyEntries);
            return nArgs;
        }
    }
}
