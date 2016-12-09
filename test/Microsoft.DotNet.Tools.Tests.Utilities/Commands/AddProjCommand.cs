// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Cli.Utils;

namespace Microsoft.DotNet.Tools.Test.Utilities
{
    public sealed class AddProjCommand : TestCommand
    {
        private string _solutionName = null;

        public AddProjCommand()
            : base("dotnet")
        {
        }

        public override CommandResult Execute(string args = "")
        {
            args = $"add {_solutionName} proj {args}";
            return base.ExecuteWithCapturedOutput(args);
        }

        public AddProjCommand WithSolution(string solutionName)
        {
            _solutionName = solutionName;
            return this;
        }
    }
}
