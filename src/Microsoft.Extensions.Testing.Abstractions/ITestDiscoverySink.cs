// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Microsoft.Extensions.Testing.Abstractions
{
    public interface ITestDiscoverySink : ITestSink
    {
        void SendTestFound(TestCase test);
    }
}