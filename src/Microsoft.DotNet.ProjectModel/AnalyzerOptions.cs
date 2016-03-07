// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;

namespace Microsoft.DotNet.ProjectModel
{
    public sealed class AnalyzerOptions : IEquatable<AnalyzerOptions>
    {
        /// <summary>
        /// The identifier indicating the project language as defined by NuGet.
        /// </summary>
        /// <remarks>
        /// See https://docs.nuget.org/create/analyzers-conventions for valid values
        /// </remarks>
        public string LanguageId { get; set; }

        public static bool operator ==(AnalyzerOptions left, AnalyzerOptions right)
        {
            return object.Equals(left, right);
        }

        public static bool operator !=(AnalyzerOptions left, AnalyzerOptions right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as AnalyzerOptions);
        }

        public bool Equals(AnalyzerOptions options)
        {
            if (options == null)
            {
                return false;
            }

            return LanguageId == options.LanguageId;
        }

        public override int GetHashCode()
        {
            return LanguageId?.GetHashCode() ?? 0;
        }
    }
}
