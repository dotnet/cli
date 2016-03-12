// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.DotNet.Cli.Compiler.Common;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ProjectModel;
using NuGet.Frameworks;

namespace Microsoft.DotNet.Tools.Compiler.Fsc
{
    public class CompileFscCommand
    {
        private const int ExitFailed = 1;

        public static int Main(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);

            var compileFscCommandApp = new CompileFscCommandApp(args);

            if (compileFscCommandApp.Help)
            {
                Console.WriteLine(compileFscCommandApp.HelpText);
                return 0;
            }

            if (compileFscCommandApp.SyntaxError)
            {
                Console.Error.WriteLine(compileFscCommandApp.SyntaxErrorMessage);
                Console.WriteLine(compileFscCommandApp.HelpText);
                return ExitFailed;
            }

            var compilationDriver = new FSharpCompilationDriver();
            return compilationDriver.Compile(compileFscCommandApp);
        }
    }
}
