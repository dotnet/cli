// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.DotNet.Utilities;

namespace Microsoft.DotNet.ProjectModel.Graph.Tests
{
    public class BuildActionTests
    {
        [Fact]
        public void Equality()
        {
            EqualityUnit
                .Create(BuildAction.Compile)
                .WithEqualValues(BuildAction.Compile)
                .WithNotEqualValues(BuildAction.Resource, BuildAction.EmbeddedResource)
                .RunAll(compEquality: (x, y) => x == y, compInequality: (x, y) => x != y);
        }
    }
}
