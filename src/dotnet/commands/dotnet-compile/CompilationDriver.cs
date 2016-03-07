// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.DotNet.ProjectModel;

namespace Microsoft.DotNet.Tools.Compiler
{
    public class CompilationDriver
    {
        private readonly IManagedCompiler _managedCompiler;

        public CompilationDriver(IManagedCompiler managedCompiler)
        {
            _managedCompiler = managedCompiler;
        }

        public bool Compile(IEnumerable<ProjectContext> contexts, CompilerCommandApp args)
        {
            var success = true;

            foreach (var context in contexts)
            {
                success &= _managedCompiler.Compile(context, args);
            }

            return success;
        }
    }
}
