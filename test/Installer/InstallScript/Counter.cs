// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.InstallScripts.Tests
{
    public class Counter<T>
    {
        public Dictionary<T, int> Data { get; private set; }
        
        public Counter()
        {
            Data = new Dictionary<T, int>();
        }
        
        public int this[T key]
        {
            get
            {
                int ret;
                if (Data.TryGetValue(key, out ret))
                {
                    return ret;
                }
                else
                {
                    return 0;
                }
            }
        }

        public void Increment(T key)
        {
            Data[key] = this[key] + 1;
        }
    }
}
