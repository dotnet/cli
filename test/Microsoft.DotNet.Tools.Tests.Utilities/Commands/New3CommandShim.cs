// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Cli.Utils;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Microsoft.DotNet.Tools.Test.Utilities
{
    public sealed class New3CommandShim : TestCommand
    {
        public New3CommandShim()
            : base("dotnet")
        {

        }

        public override CommandResult Execute(string args = "")
        {
            args = $"new3 {args}";
            return base.Execute(args);
        }

        public override CommandResult ExecuteWithCapturedOutput(string args = "")
        {
            args = $"new3 {args}";
            return base.ExecuteWithCapturedOutput(args);
        }
    }
}
