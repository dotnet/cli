// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.ProjectModel.Graph;

namespace Microsoft.DotNet.ProjectModel.Compilation
{
    public class LibraryContentFile
    {
        public string Path { get; set; }

        public string OutputPath { get; set; }

        public bool CopyToOutput { get; set; }

        public BuildAction BuildAction { get; set; } = BuildAction.None;

        public bool Preprocess { get; set; }

        public string CodeLanguage { get; set; }
    }
}