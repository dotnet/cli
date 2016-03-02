// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.DotNet.Utilities;

namespace Microsoft.DotNet.ProjectModel.Tests
{
    public class AnalyzerOptionsTests
    {
        [Fact]
        public void Equality_Null()
        {
            EqualityUnit
                .Create(new AnalyzerOptions())
                .WithEqualValues(new AnalyzerOptions())
                .WithNotEqualValues(new AnalyzerOptions() { LanguageId = "csharp"})
                .RunAll(compEquality: (x, y) => x == y, compInequality: (x, y) => x != y);
        }

        [Fact]
        public void Equality_Basic()
        {
            EqualityUnit
                .Create(new AnalyzerOptions() { LanguageId = "csharp" })
                .WithEqualValues(new AnalyzerOptions() { LanguageId = "csharp" })
                .WithNotEqualValues(new AnalyzerOptions() { LanguageId = "vb" })
                .RunAll(compEquality: (x, y) => x == y, compInequality: (x, y) => x != y);
        }
    }
}
