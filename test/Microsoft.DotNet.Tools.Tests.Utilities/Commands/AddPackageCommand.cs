// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Cli.Utils;

namespace Microsoft.DotNet.Tools.Test.Utilities
{
    public sealed class AddPackageCommand : DotnetCommand
    {
        private string _projectName = null;

        public override CommandResult Execute(string args = "")
        {
            args = $"add {_projectName} package {args}";
            return base.ExecuteWithCapturedOutput(args);
        }

        public AddPackageCommand WithProject(string projectName)
        {
            _projectName = projectName;
            return this;
        }
    }
}
