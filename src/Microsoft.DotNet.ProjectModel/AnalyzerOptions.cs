// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.ProjectModel
{
    public class AnalyzerOptions
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
            return left.LanguageId == right.LanguageId;
        }

        public static bool operator !=(AnalyzerOptions left, AnalyzerOptions right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
            {
                return false;
            }
            if (ReferenceEquals(obj, this))
            {
                return true;
            }

            var options = obj as AnalyzerOptions;
            return options != null && options.LanguageId == LanguageId;
        }

        public override int GetHashCode()
        {
            return LanguageId.GetHashCode();
        }
    }
}
