// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.DotNet.ProjectModel;

namespace Microsoft.DotNet.Tools.Compiler.Native
{
    public class NativeCompilationDriver
    {
        private readonly NativeCompilerImpl _nativeCompiler;

        public NativeCompilationDriver(NativeCompilerImpl nativeCompiler)
        {
            _nativeCompiler = nativeCompiler;
        }

        public bool Compile(IEnumerable<ProjectContext> contexts, NativeCompilerCommandApp args)
        {
            var success = true;

            foreach (var context in contexts)
            {
                var runtimeContext = context.CreateRuntimeContext(args.GetRuntimes());
                success &= _nativeCompiler.Compile(runtimeContext, args);
            }

            return success;
        }
    }
}
