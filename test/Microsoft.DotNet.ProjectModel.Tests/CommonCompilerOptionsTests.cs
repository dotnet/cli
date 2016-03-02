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
    public class CommonCompilerOptionsTests
    {
        [Fact]
        public void Equality1()
        {
            EqualityUnit
                .Create(new CommonCompilerOptions() { PublicSign = true })
                .WithEqualValues(new CommonCompilerOptions() { PublicSign = true })
                .WithNotEqualValues(new CommonCompilerOptions() { PublicSign = false })
                .RunAll();
        }

        [Fact]
        public void Equality_Defines()
        {
            EqualityUnit
                .Create(new CommonCompilerOptions() { PublicSign = true, Defines = new[] { "a", "b" } })
                .WithEqualValues(new CommonCompilerOptions() { PublicSign = true, Defines = new[] {"a", "b" } })
                .WithNotEqualValues(
                    new CommonCompilerOptions() { PublicSign = false },
                    new CommonCompilerOptions() { PublicSign = true, Defines = new[] { "a" } },
                    new CommonCompilerOptions() { PublicSign = true, Defines = new[] { "b", "a" } },
                    new CommonCompilerOptions() { PublicSign = true, Defines = new[] { "A", "B" } })
                .RunAll();
        }
    }
}
