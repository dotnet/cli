// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.Dnx.TestHost
{
    public static class TestHostTracing
    {
        public static readonly string TracingEnvironmentVariable = "DNX_TESTHOST_TRACE";

        public static readonly TraceSource Source;

        static TestHostTracing()
        {
            if (Environment.GetEnvironmentVariable(TracingEnvironmentVariable) == "1")
            {
                Source = new TraceSource("Microsoft.Dnx.TestHost", SourceLevels.Verbose);
            }
            else
            {
                Source = new TraceSource("Microsoft.Dnx.TestHost", SourceLevels.Warning);
            }

            Source.Listeners.Add(new TextWriterTraceListener(Console.Error));
        }
    }
}
