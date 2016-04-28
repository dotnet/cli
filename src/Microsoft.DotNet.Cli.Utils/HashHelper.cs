// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.DotNet.Cli.Utils
{
    public static class HashHelper
    {
        public static IList<string> Sha256HashList(IList<string> values)
        {
            StringBuilder Sb = new StringBuilder();

            using (SHA256 hash = SHA256.Create())
            {
                for (int i = 0; i < values.Count; i++)
                {
                    Sb.Clear();
                    Encoding enc = Encoding.UTF8;
                    byte[] result = hash.ComputeHash(enc.GetBytes(values[i]));

                    foreach (byte b in result)
                    {
                        Sb.Append(b.ToString("x2"));
                    }
                    values[i] = Sb.ToString();
                }
            }

            return values;
        }
    }
}
