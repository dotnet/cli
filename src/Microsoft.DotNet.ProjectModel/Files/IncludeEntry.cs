﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.ProjectModel.Files
{
    public class IncludeEntry : IEquatable<IncludeEntry>
    {
        public string TargetPath { get; }

        public string SourcePath { get; }

        public IncludeEntry(string target, string source)
        {
            TargetPath = target;
            SourcePath = source;
        }

        public override bool Equals(object obj)
        {
            return Equals((IncludeEntry)obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public bool Equals(IncludeEntry other)
        {
            return other != null &&
                TargetPath == other.TargetPath &&
                SourcePath == other.SourcePath;
        }
    }
}
