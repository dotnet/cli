﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Tools.Test.Utilities
{
    public static class FileInfoExtensions
    {
        public static FileInfoAssertions Should(this FileInfo fileInfo)
        {
            return new FileInfoAssertions(fileInfo);
        }
    }
}
