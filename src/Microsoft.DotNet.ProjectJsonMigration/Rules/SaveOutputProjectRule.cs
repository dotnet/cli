// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Microsoft.Build.Construction;

namespace Microsoft.DotNet.ProjectJsonMigration.Rules
{
    internal class SaveOutputProjectRule : IMigrationRule
    {
        public void Apply(MigrationSettings migrationSettings, MigrationRuleInputs migrationRuleInputs)
        {
            var outputName = migrationRuleInputs.DefaultProjectContext.GetProjectName();

            var outputProject = Path.Combine(migrationSettings.OutputDirectory, outputName + ".csproj");

            CleanEmptyPropertyAndItemGroups(migrationRuleInputs.OutputMSBuildProject);

            migrationRuleInputs.OutputMSBuildProject.Save(outputProject);
        }

        private void CleanEmptyPropertyAndItemGroups(ProjectRootElement msbuildProject)
        {
            foreach (var propertyGroup in msbuildProject.PropertyGroups)
            {
                propertyGroup.RemoveIfEmpty();
            }

            foreach (var itemGroup in msbuildProject.ItemGroups)
            {
                itemGroup.RemoveIfEmpty();
            }
        }
    }
}
