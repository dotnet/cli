// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Text;

namespace Microsoft.DotNet.InstallScripts.Tests
{
    public static class StreamUtils
    {
        public static void WriteAllText(this Stream s, string text)
        {
            using (StreamWriter sw = new StreamWriter(s, Encoding.UTF8, 8192, leaveOpen: true))
            {
                sw.Write(text);
            }
        }

        public static string ReadAllText(this Stream s)
        {
            using (StreamReader sr = new StreamReader(s, Encoding.UTF8, true, 8192, leaveOpen: true))
            {
                return sr.ReadToEnd();
            }
        }
    }
}
