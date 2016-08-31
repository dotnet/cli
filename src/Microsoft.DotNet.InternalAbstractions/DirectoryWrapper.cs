﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using Microsoft.DotNet.InternalAbstractions;

namespace Microsoft.Extensions.EnvironmentAbstractions
{
    internal class DirectoryWrapper: IDirectory
    {
        public bool Exists(string path)
        {
            return Directory.Exists(path);
        }

        public ITemporaryDirectory CreateTemporaryDirectory()
        {
            return new TemporaryDirectory();
        }

        public IEnumerable<string> GetFiles(string path, string searchPattern)
        {
            return Directory.GetFiles(path, searchPattern);
        }

        public string GetDirectoryFullName(string path)
        {
            var directoryFullName = string.Empty;
            if (Exists(path))
            {
                directoryFullName = new DirectoryInfo(path).FullName;
            }
            else
            {
                var fileInfo = new FileInfo(path);
                if (fileInfo.Directory != null)
                {
                    directoryFullName = fileInfo.Directory.FullName;
                }
            }

            return directoryFullName;
        }
    }
}