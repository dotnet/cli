﻿using System;
using System.Text;
using Microsoft.Extensions.Internal;
using NuGet.Versioning;

namespace Microsoft.Extensions.ProjectModel.Graph
{
    public struct LibraryRange : IEquatable<LibraryRange>
    {
        public string Name { get; }
        public VersionRange VersionRange { get; }
        public LibraryType Target { get; }
        public LibraryDependencyType Type { get; }

        public string SourceFilePath { get; }
        public int SourceLine { get; }
        public int SourceColumn { get; }

        public LibraryRange(string name, LibraryType target)
            : this(name, null, target, LibraryDependencyType.Default, sourceFilePath: string.Empty, sourceLine: 0, sourceColumn: 0)
        { }

        public LibraryRange(string name, LibraryType target, LibraryDependencyType type)
            : this(name, null, target, type, sourceFilePath: string.Empty, sourceLine: 0, sourceColumn: 0)
        { }

        public LibraryRange(string name, VersionRange versionRange, LibraryType target, LibraryDependencyType type)
            : this(name, versionRange, target, type, sourceFilePath: string.Empty, sourceLine: 0, sourceColumn: 0)
        { }

        public LibraryRange(string name, VersionRange versionRange, LibraryType target, LibraryDependencyType type, string sourceFilePath, int sourceLine, int sourceColumn)
        {
            Name = name;
            VersionRange = versionRange;
            Target = target;
            Type = type;
            SourceFilePath = sourceFilePath;
            SourceLine = sourceLine;
            SourceColumn = sourceColumn;
        }

        public bool Equals(LibraryRange other)
        {
            return string.Equals(other.Name, Name, StringComparison.OrdinalIgnoreCase) &&
                Equals(VersionRange, other.VersionRange) &&
                Equals(Target, other.Target) &&
                Equals(Type, other.Type);
            // SourceFilePath, SourceLine, SourceColumn are irrelevant for equality, they are diagnostic
        }

        public override bool Equals(object obj)
        {
            return obj is LibraryRange && Equals((LibraryRange)obj);
        }

        public static bool operator ==(LibraryRange left, LibraryRange right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(LibraryRange left, LibraryRange right)
        {
            return !Equals(left, right);
        }

        public override int GetHashCode()
        {
            var combiner = HashCodeCombiner.Start();
            combiner.Add(Name);
            combiner.Add(VersionRange);
            combiner.Add(Target);
            combiner.Add(Type);
            return combiner.CombinedHash;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Name);

            if (VersionRange != null)
            {
                sb.Append(" ");
                sb.Append(VersionRange);
            }

            if (!Equals(Type, LibraryDependencyType.Default))
            {
                sb.Append(" (");
                sb.Append(Type);
                sb.Append(")");
            }

            if (!Equals(Target, LibraryType.Unspecified))
            {
                sb.Append(" (Target: ");
                sb.Append(Target);
                sb.Append(")");
            }

            return sb.ToString();
        }

        public bool HasFlag(LibraryDependencyTypeFlag flag)
        {
            return Type.HasFlag(flag);
        }
    }
}