// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Tools.Build
{
    internal class IncrementalResult
    {
        public static readonly IncrementalResult ShouldSkipBuild = new IncrementalResult(true, "", Enumerable.Empty<string>());

        public bool SkipBuild { get; }
        public string Reason { get; }
        public IEnumerable<string> Items { get; }

        private IncrementalResult(bool skipBuild, string reason, IEnumerable<string> items)
        {
            SkipBuild = skipBuild;
            Reason = reason;
            Items = items;
        }

        public IncrementalResult(string reason)
            : this(false, reason, Enumerable.Empty<string>())
        {
        }

        public IncrementalResult(string reason, IEnumerable<string> items)
            : this(false, reason, items)
        {
        }
    }
}