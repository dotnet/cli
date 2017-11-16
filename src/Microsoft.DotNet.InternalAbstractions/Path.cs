// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.Extensions.EnvironmentAbstractions
{
    public class DirectoryPath
    {
        public string Value { get; }

        public DirectoryPath(string value)
        {
            Value = value;
        }

        public DirectoryPath WithCombineFollowing(params string[] paths)
        {
            string[] insertValueInFront = new string[paths.Length + 1];
            insertValueInFront[0] = Value;
            Array.Copy(paths, 0, insertValueInFront, 1, paths.Length);
            
            return new DirectoryPath(Path.Combine(insertValueInFront));
        }

        public FilePath CreateFilePathWithCombineFollowing(string fileName)
        {
            return new FilePath(Path.Combine(Value, fileName));
        }

        public string ToEscapedString()
        {
            return $"\"{Value}\"";
        }

        public override string ToString()
        {
            return ToEscapedString();
        }

        public DirectoryPath GetParentPath()
        {
            return new DirectoryPath(Directory.GetParent(Path.GetFullPath(Value)).FullName);
        }
    }

    public class FilePath
    {
        public string Value { get; }

        public FilePath(string value)
        {
            Value = value;
        }
        
        public string ToEscapedString()
        {
            return $"\"{Value}\"";
        }

        public override string ToString()
        {
            return ToEscapedString();
        }

        public DirectoryPath GetDirectoryPath()
        {
            return new DirectoryPath(Path.GetDirectoryName(Value));
        }
    }
}
