// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reflection.PortableExecutable;
using Microsoft.DotNet.Cli.Utils;

namespace Microsoft.DotNet.Tools.CrossGen.Operations
{
    public static class PEUtils
    {
        public static bool HasMetadata(string pathToFile)
        {
            var hasMetadata = false;
            try
            {
                using (var inStream = File.OpenRead(pathToFile))
                using (var peReader = new PEReader(inStream))
                {
                    hasMetadata = peReader.HasMetadata;
                }
            }
            catch (BadImageFormatException) { }

            if (!hasMetadata)
            {
                Reporter.Verbose.WriteLine($"No metadata was found for {pathToFile}");
            }

            return hasMetadata;
        }
    }
}
