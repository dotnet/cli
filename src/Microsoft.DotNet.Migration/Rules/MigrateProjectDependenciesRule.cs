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

namespace Microsoft.DotNet.Migration.Rules
{
    public class MigrateProjectDependenciesRule : IMigrationRule
    {
        public void Apply(ProjectContext projectContext, ProjectRootElement csproj, string outputDirectory)
        {
            // Find Project Dependencies
            var projectExports = projectContext.CreateExporter(null).GetDependencies(LibraryType.Project);

            // Create ItemGroup
            var itemGroup = csproj.AddItemGroup();

            // Populate ProjectReference Include
            foreach (var projectExport in projectExports)
            {
                ProjectDependencyTransform.Execute(projectExport, csproj, itemGroup);
            }
        }

        private ITransform<LibraryExport> ProjectDependencyTransform = new AddItemTransform<LibraryExport>(
            "ProjectReference", 
            export => ((ProjectDescription)export.Library).Project.ProjectFilePath,
            export => "",
            export => true);
    }
}
