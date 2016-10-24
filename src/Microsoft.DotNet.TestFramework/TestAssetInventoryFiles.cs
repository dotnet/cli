// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.DotNet.Cli.Utils;

namespace Microsoft.DotNet.TestFramework
{
    public class TestAssetInventoryFiles
    {
        public FileInfo Source { get; private set; }

        public FileInfo Restore { get; private set; }

        public FileInfo Build { get; private set; }

        public TestAssetInventoryFiles(DirectoryInfo inventoryFileDirectory)
        {
            Source = new FileInfo(Path.Combine(inventoryFileDirectory.FullName, "source.txt"));

            Restore = new FileInfo(Path.Combine(inventoryFileDirectory.FullName, "restore.txt"));

            Build = new FileInfo(Path.Combine(inventoryFileDirectory.FullName, "build.txt"));
        }
    }
}
