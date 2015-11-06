// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;

namespace Microsoft.Dnx.Testing.Abstractions
{
    public abstract class TestHostServices
    {
        public abstract ITestDiscoverySink TestDiscoverySink { get; }

        public abstract ITestExecutionSink TestExecutionSink { get; }

        public abstract ISourceInformationProvider SourceInformationProvider { get; }

        public abstract ILoggerFactory LoggerFactory { get; }
    }
}