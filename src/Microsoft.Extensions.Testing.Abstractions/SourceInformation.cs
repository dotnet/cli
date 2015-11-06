// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Dnx.Testing.Abstractions
{
    public class SourceInformation
    {
        public SourceInformation(string filename, int lineNumber)
        {
            Filename = filename;
            LineNumber = lineNumber;
        }

        public string Filename { get; }

        public int LineNumber { get; }
    }
}