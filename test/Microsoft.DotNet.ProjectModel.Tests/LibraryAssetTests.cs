// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.DotNet.Utilities;

namespace Microsoft.DotNet.ProjectModel.Compilation.Tests
{
    public class LibraryAssetTests
    {
        [Fact]
        public void Equality()
        {
            EqualityUnit
                .Create(new LibraryAsset("a", "b", "c", transform: null))
                .WithEqualValues(
                    new LibraryAsset("a", "b", "c", transform: null),
                    new LibraryAsset("a", "b", "c", transform: (x, y) => { }))
                .WithNotEqualValues(
                    new LibraryAsset("A", "b", "c", transform: null),
                    new LibraryAsset("a", "B", "c", transform: (x ,y) => { }),
                    new LibraryAsset("a", "B", "C", transform: (x, y) => { }),
                    new LibraryAsset(null, null, null, transform: (x, y) => { }))
                .RunAll();
        }
    }
}
