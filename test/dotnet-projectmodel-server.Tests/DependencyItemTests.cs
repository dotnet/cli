// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.DotNet.Utilities;

namespace Microsoft.DotNet.ProjectModel.Server.Models.Tests
{
    public class DependencyItemTests
    {
        [Fact]
        public void Equality_Name()
        {
            EqualityUnit
                .Create(new DependencyItem() { Name = "bob" })
                .WithEqualValues(
                    new DependencyItem() { Name = "bob" },
                    new DependencyItem() { Name = "bob", Version = null })
                .WithNotEqualValues(
                    new DependencyItem() { Name = "Bob" },
                    new DependencyItem() { Name = "bob", Version = "" },
                    new DependencyItem() { Name = "jim", Version = "" })
                .RunAll();
        }

        [Fact]
        public void Equality_Version()
        {
            EqualityUnit
                .Create(new DependencyItem() { Version = "bob" })
                .WithEqualValues(
                    new DependencyItem() { Version = "bob" },
                    new DependencyItem() { Version = "bob", Name = null })
                .WithNotEqualValues(
                    new DependencyItem() { Version = "bob", Name = "" },
                    new DependencyItem() { Version = "jim", Name = "" })
                .RunAll();
        }

        [Fact]
        public void Equality_Both()
        {
            EqualityUnit
                .Create(new DependencyItem() { Name = "bob", Version = "new" })
                .WithEqualValues(
                    new DependencyItem() { Name = "bob", Version = "new" })
                .WithNotEqualValues(
                    new DependencyItem() { Name = "bob", Version = "NEW" },
                    new DependencyItem() { Name = "BOB", Version = "new" })
                .RunAll();
        }
    }
}
