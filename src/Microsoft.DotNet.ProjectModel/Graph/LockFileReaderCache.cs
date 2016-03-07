// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.DotNet.ProjectModel.Graph
{
    public class LockFileReaderCache
    {
        private readonly Dictionary<string, LockFile> _cache = new Dictionary<string, LockFile>();

        public LockFile Read(string lockFilePath)
        {
            lock (_cache)
            {
                LockFile lockFile;
                if (!_cache.TryGetValue(lockFilePath, out lockFile))
                {
                    lockFile = LockFileReader.Read(lockFilePath);
                    _cache.Add(lockFilePath, lockFile);
                }
                return lockFile;
            }
        }
    }
}