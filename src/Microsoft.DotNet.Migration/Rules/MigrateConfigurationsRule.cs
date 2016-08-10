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

namespace Microsoft.DotNet.ProjectJsonMigration
{
    public class MigrateConfigurationsRule : IMigrationRule
    {
        public void Apply(ProjectContext projectContext, ProjectRootElement csproj, string outputDirectory)
        {
            var configurations = projectContext.ProjectFile.GetConfigurations();

            if (configurations.Count() == 0)
            {
                return;
            }

            foreach (var configuration in configurations)
            {
                MigrateConfiguration(configuration, projectContext, csproj, outputDirectory);
            }
        }

        private void MigrateConfiguration(string configuration, ProjectContext projectContext, ProjectRootElement csproj, string outputDirectory)
        {
            var configurationCondition = $" '$(Configuration)' == '{configuration}' ";

            var propertyGroup = csproj.AddPropertyGroup();
            var itemGroup = csproj.AddItemGroup();

            propertyGroup.Condition = configurationCondition;
            itemGroup.Condition = configurationCondition;

            var migrateBuildOptionsInConfigurationRule = new MigrateBuildOptionsRule(propertyGroup, itemGroup);
            migrateBuildOptionsInConfigurationRule.Apply(projectContext, csproj, outputDirectory);
        }
    }
}
