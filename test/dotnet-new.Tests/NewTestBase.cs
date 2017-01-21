// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Microsoft.DotNet.New.Tests
{
    public class NewTestBase : TestBase
    {
        private static readonly object InitializationSync = new object();
        private static bool _isInitialized;

        protected NewTestBase()
        {
            if (_isInitialized)
            {
                return;
            }

            lock (InitializationSync)
            {
                if (_isInitialized)
                {
                    return;
                }

                //Force any previously computed configuration to be cleared
                new TestCommand("dotnet").Execute("new --debug:reinit");
                _isInitialized = true;
            }
        }
    }
}
