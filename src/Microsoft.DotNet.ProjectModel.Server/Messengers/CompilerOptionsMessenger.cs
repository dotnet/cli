// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.DotNet.Cli.Compiler.Common;
using Microsoft.DotNet.ProjectModel.Server.Models;

namespace Microsoft.DotNet.ProjectModel.Server.Messengers
{
    internal class CompilerOptionsMessenger : Messenger<ProjectContextSnapshot>
    {
        public CompilerOptionsMessenger(Action<string, object> transmit)
            : base(MessageTypes.CompilerOptions, transmit)
        { }

        protected override bool CheckDifference(ProjectContextSnapshot local, ProjectContextSnapshot remote)
        {
            return remote.CompilerOptions != null &&
                   Equals(local.CompilerName, remote.CompilerName) &&
                   Equals(local.CompilerOptions, remote.CompilerOptions);
        }

        protected override object CreatePayload(ProjectContextSnapshot local)
        {
            var option = CommonCompilerOptions.Combine(local.CompilerOptions, new CommonCompilerOptions
            {
                SuppressWarnings = DefaultCompilerWarningSuppresses.Instance[local.CompilerName]
            });

            return new CompilationOptionsMessage
            {
                Framework = local.TargetFramework.ToPayload(),
                Options = option
            };
        }

        protected override void SetValue(ProjectContextSnapshot local, ProjectContextSnapshot remote)
        {
            remote.CompilerOptions = local.CompilerOptions;
        }

        private class CompilationOptionsMessage
        {
            public FrameworkData Framework { get; set; }

            public CommonCompilerOptions Options { get; set; }
        }
    }
}
