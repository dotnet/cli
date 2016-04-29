// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.DotNet.ProjectModel.FileSystemGlobbing.Abstractions
{
    public class FileInfoWrapper : FileInfoBase
    {
        private FileInfo _fileInfo;

        public FileInfoWrapper(FileInfo fileInfo)
        {
            _fileInfo = fileInfo;
        }

        public override string Name
        {
            get { return _fileInfo.Name; }
        }

        public override string FullName
        {
            get { return _fileInfo.FullName; }
        }

        public override DirectoryInfoBase ParentDirectory
        {
            get { return new DirectoryInfoWrapper(_fileInfo.Directory); }
        }
    }
}