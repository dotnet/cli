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
    public class ErrorMessageTests
    {
        [Fact]
        public void Equality_Message()
        {
            EqualityUnit
                .Create(new ErrorMessage () { Name = "hello" })
                .WithEqualValues(
                    new ErrorMessage() { Name = "hello" },
                    new ErrorMessage() { Name = "hello", Line = 0, Column = 0 })
                .WithNotEqualValues(
                    new ErrorMessage() { Name = "HELLO" },
                    new ErrorMessage() { Name = "hello", Line = 1, Column = 0 })
                .RunAll();
        }

        [Fact]
        public void Equality_Span()
        {
            EqualityUnit
                .Create(new ErrorMessage () { Line = 1, Column = 2 })
                .WithEqualValues(
                    new ErrorMessage() { Line = 1, Column = 2 },
                    new ErrorMessage() { Line = 1, Column = 2, Name = null },
                .WithNotEqualValues(
                    new ErrorMessage() { Line = 2, Column = 2 },
                    new ErrorMessage() { Line = 1, Column = 1 },
                    new ErrorMessage() { Line = 1, Column = 2, Name = "hello" })
                .RunAll();
        }

        [Fact]
        public void Equality_Path()
        {
            EqualityUnit
                .Create(new ErrorMessage () { Path = "file.txt" })
                .WithEqualValues(
                    new ErrorMessage() { Path = "file.txt" },
                    new ErrorMessage() { Path = "FILE.txt" })
                .WithNotEqualValues(
                    new ErrorMessage() { Path = "FILE.txt", Name = "hello" })
                .RunAll();
        }
    }
}
