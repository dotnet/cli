// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Construction;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.Cli;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using Microsoft.DotNet.ProjectJsonMigration.Rules;

namespace Microsoft.DotNet.ProjectJsonMigration
{
    public class DefaultMigrationRuleSet : IMigrationRule
    {
        private IMigrationRule[] Rules => new IMigrationRule[]
        {
            new MigrateBuildOptionsRule(),
            new MigrateRuntimeOptionsRule(),
            new MigratePublishOptionsRule(),
            new MigrateProjectDependenciesRule(),
            new MigrateConfigurationsRule()
        };

        public void Apply(ProjectContext projectContext, ProjectRootElement csproj, string outputDirectory)
        {
            foreach (var rule in Rules)
            {
                rule.Apply(projectContext, csproj, outputDirectory);
            }
        }
    }
}
