// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.TestHost
{
    public class LogMessage
    {
        public string Name { get; set; }

        public int EventId { get; set; }

        public string Level { get; set; }

        public string Message { get; set; }
    }
}
