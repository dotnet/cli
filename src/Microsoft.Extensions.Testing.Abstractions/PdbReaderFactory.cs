// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.Extensions.Testing.Abstractions
{
    public class PdbReaderFactory : IPdbReaderFactory
    {
        public IPdbReader Create(string pdbPath)
        {
            var pdbStream = new FileStream(pdbPath, FileMode.Open, FileAccess.Read);

            if (pdbStream.IsPortable())
            {
                return new PortablePdbReader(pdbStream);
            }
            else
            {
                return new FullPdbReader(pdbStream);
            }
        }
    }
}
