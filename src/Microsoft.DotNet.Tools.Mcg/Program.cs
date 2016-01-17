// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.DotNet.Cli.Utils;
using System.CommandLine;

namespace Microsoft.DotNet.Tools.Compiler.Mcg
{
    public class Program
    {        
        private readonly static string HostExecutableName = "corerun" + Constants.ExeSuffix;
        private readonly static string McgBinaryName = "mcg.exe";        
        private static IReadOnlyList<string> references = Array.Empty<string>();
        private static string inputAssembly;
        private static string targetArch;
        private static string outputPath;

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

            var nArgs = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            return nArgs;
        }

        private static string TransformCommandLine(string[] args)
        {
            StringBuilder mcgArgs = new StringBuilder(512);

            //TODO : Mcg argument need to be consistent with ilc.exe , also expose all mcg arguments         
            const string mcgArgFormat = "{0} -p:{1} -referencepath:{2} -outputpath:{3}";
            ArgumentSyntax.Parse(args, syntax =>
            {
                syntax.HandleHelp = false;
                syntax.HandleErrors = false;
                syntax.DefineOption("p", ref targetArch, "Target architecture.");
                syntax.DefineOptionList("reference", ref references, "Use to specify Managed DLL references of the app.");
                syntax.DefineOption("outputpath", ref outputPath, "Mcg output path");
                syntax.DefineParameter("INPUT_ASSEMBLY", ref inputAssembly, "The managed input assembly.");
                string helpText = syntax.GetHelpText();
                if (string.IsNullOrWhiteSpace(inputAssembly))
                {
                    syntax.ReportError("Input Assembly is a required parameter.");
                }
            });

            // TODO : References necessary to resolve types and compile mcg generated code.This should 
            // come from nuget.
            string referencepath = Path.Combine(AppContext.BaseDirectory, "mcg", "ref");
            mcgArgs.AppendFormat(mcgArgFormat, inputAssembly, targetArch, referencepath, outputPath);
            return mcgArgs.ToString();
        }

        public static int Main(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);

            // Support Response File
            foreach (var arg in args)
            {
                if (arg.Contains(".rsp"))
                {
                    args = ParseResponseFile(arg);

                    if (args == null)
                    {
                        return 1;
                    }
                }
            }
            string mcgArguments = TransformCommandLine(args);
            return ExecuteMcg(mcgArguments);
        }

        private static int ExecuteMcg(string arguments)
        {
            // TODO : Mcg is assumed to be present under the current path.This will change once 
            // we have mcg working on coreclr and is available as a nuget package.                                 
            var executablePath = Path.Combine(@"mcg", McgBinaryName);
            Console.WriteLine(arguments);
            var result = Command.Create(executablePath, arguments)
                .ForwardStdErr()
                .ForwardStdOut()
                .Execute();
            return result.ExitCode;
        }
   }  
}
