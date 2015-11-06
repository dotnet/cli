// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Dnx.Testing.Abstractions
{
    public interface ITestDiscoverySink
    {
        void SendTest(Test test);
    }
}