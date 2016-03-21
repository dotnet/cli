// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Microsoft.Extensions.Testing.Abstractions
{
    internal static class StreamExtensions
    {
        internal static bool IsPortable(this Stream pdbStream)
        {
            return pdbStream.ReadByte() == 'B' &&
                pdbStream.ReadByte() == 'S' &&
                pdbStream.ReadByte() == 'J' &&
                pdbStream.ReadByte() == 'B';
        }
    }
}
