﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.DotNet.Cli.Compiler.Common
{
    public class DefaultCompilerWarningSuppresses
    {
        private static IReadOnlyDictionary<string, IReadOnlyList<string>> _suppresses = new Dictionary<string, IReadOnlyList<string>>
        {
            { "csc", new string[] {"CS1701", "CS1702", "CS1705" } }
        };

        public static IReadOnlyDictionary<string, IReadOnlyList<string>> Instance => _suppresses;
    }
}
