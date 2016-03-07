// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli.Utils;

namespace Microsoft.DotNet.Tools.Compiler.Native
{
    public class CommpileNativeCommand
    {

        public static int Main(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);

            try
            {
                var commandFactory = new DotNetCommandFactory();
                var nativeCompiler = new NativeCompilerImpl();
                var nativeCompilationDriver = new NativeCompilationDriver(nativeCompiler);

                var nativeCompilerCommandApp = new NativeCompilerCommandApp("dotnet-compile-native", ".NET Native Compiler", "Native Compiler for the .NET Platform");

                return nativeCompilerCommandApp.Execute(nativeCompilationDriver.Compile, args);
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.Error.WriteLine(ex);
#else
                Console.Error.WriteLine(ex.Message);
#endif
                return 1;
            }
        }
    }
}
