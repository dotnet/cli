// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Cli.Utils;

namespace Microsoft.DotNet.Tools.Test.Utilities
{
    public sealed class RestoreCommand : TestCommand
    {
        private List<string> _fallbackSources;

        public RestoreCommand()
            : base("dotnet")
        {
        }

        public RestoreCommand WithFallbackSource(string source)
        {
            if (_fallbackSources == null)
            {
                _fallbackSources = new List<string>();
            }

            _fallbackSources.Add(source);

            return this;
        }

        public override CommandResult Execute(string args="")
        {
            args = $"restore {args}{GetFallbackSourceArgs()}";

            return base.Execute(args);
        }

        private string GetFallbackSourceArgs()
        {
            if (_fallbackSources != null)
            {
                return " " + string.Join(" ", _fallbackSources.Select(f => $"--fallbacksource {f}"));
            }

            return null;
        }
    }
}
