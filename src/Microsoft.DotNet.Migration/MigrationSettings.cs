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

namespace Microsoft.DotNet.Migration
{
    public class MigrationSettings
    {
        public string ProjectDirectory { get; }
        public string OutputDirectory { get; }
        public bool RecurseProjectDependencies { get; }

        public MigrationSettings(string projectDirectory, string outputDirectory, bool recurseProjectDependencies=false)
        {
            ProjectDirectory = projectDirectory;
            OutputDirectory = outputDirectory;
            RecurseProjectDependencies = recurseProjectDependencies;
        }
    }
}
