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
    public class McgCommand
    {        
        private readonly static string HostExecutableName = "corerun" + Constants.ExeSuffix;
        private readonly static string McgBinaryName = "Mcg.exe"; 

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

        public static int Run(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);
            return ExecuteMcg(args);
        }

        private static int ExecuteMcg(IEnumerable<string> arguments)
        {            
            var executablePath = Path.Combine(AppContext.BaseDirectory, McgBinaryName);
            
            var result = Command.Create(executablePath, arguments)
                .ForwardStdErr()
                .ForwardStdOut()
                .Execute();
            return result.ExitCode;
        }
   }  
}
