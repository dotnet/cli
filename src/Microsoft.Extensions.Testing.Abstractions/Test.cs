// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Dnx.Testing.Abstractions
{
    public class Test
    {
        public Test()
        {
            Properties = new Dictionary<string, object>(StringComparer.Ordinal);
        }

        public string CodeFilePath { get; set; }

        public string DisplayName { get; set; }

        public string FullyQualifiedName { get; set; }

        public Guid? Id { get; set; }

        public int? LineNumber { get; set; }

        public IDictionary<string, object> Properties { get; private set; }
    }
}