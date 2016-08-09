// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Construction;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Cli;
using System.Linq;
using System.IO;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Migration.Transforms
{
    public class AddStringPropertyTransform : AddPropertyTransform<string> 
    {
        public AddStringPropertyTransform(string propertyName)
            : base(propertyName, s => s, s => !string.IsNullOrEmpty(s)) { }

        public AddStringPropertyTransform(string propertyName, Func<string, bool> condition)
            : base(propertyName, s => s, condition) { }
    }
}
