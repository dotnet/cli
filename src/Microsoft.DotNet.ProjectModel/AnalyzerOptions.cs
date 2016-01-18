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
    }
}