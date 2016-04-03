// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.JsonParser.Sources;

namespace Microsoft.DotNet.ProjectModel
{
    public class PackIncludeEntry
    {
        public string Target { get; }
        public string[] SourceGlobs { get; }
        public int Line { get; }
        public int Column { get; }

        internal PackIncludeEntry(string target, JsonValue json)
            : this(target, ExtractValues(json), json.Line, json.Column)
        {
        }

        public PackIncludeEntry(string target, string[] sourceGlobs, int line, int column)
        {
            Target = target;
            SourceGlobs = sourceGlobs;
            Line = line;
            Column = column;
        }

        public override bool Equals(object obj)
        {
            var other = obj as PackIncludeEntry;
            return other != null &&
                Target == other.Target &&
                EnumerableEquals(SourceGlobs, other.SourceGlobs);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        private static bool EnumerableEquals(IEnumerable<string> left, IEnumerable<string> right)
            => Enumerable.SequenceEqual(left ?? EmptyArray<string>.Value, right ?? EmptyArray<string>.Value);

        private static string[] ExtractValues(JsonValue json)
        {
            var valueAsString = json as JsonString;
            if (valueAsString != null)
            {
                return new string[] { valueAsString.Value };
            }

            var valueAsArray = json as JsonArray;
            if(valueAsArray != null)
            {
                return valueAsArray.Values.Select(v => v.ToString()).ToArray();
            }
            return new string[0];
        }
    }
}
