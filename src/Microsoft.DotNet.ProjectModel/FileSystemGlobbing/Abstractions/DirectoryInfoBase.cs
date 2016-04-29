// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.DotNet.ProjectModel.FileSystemGlobbing.Abstractions
{
    public abstract class DirectoryInfoBase : FileSystemInfoBase
    {
        public abstract IEnumerable<FileSystemInfoBase> EnumerateFileSystemInfos();

        public abstract DirectoryInfoBase GetDirectory(string path);

        public abstract FileInfoBase GetFile(string path);
    }
}