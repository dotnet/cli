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
using Microsoft.DotNet.ProjectModel.Files;

namespace Microsoft.DotNet.ProjectJsonMigration
{
    public class MigratePublishOptionsRule : IMigrationRule
    {
        // TODO: Support mappings
        // TODO: remove any existing items in the template when appropriate
        // TODO: specify ordering of generated property/item groups (append at end of file in most cases)
        public void Apply(ProjectContext projectContext, ProjectRootElement csproj, string outputDirectory)
        {
            var itemGroup = csproj.AddItemGroup();
            CopyToOutputFilesTransform.Execute(projectContext.ProjectFile.PublishOptions, csproj, itemGroup);
        }

        private ITransform<IncludeContext> CopyToOutputFilesTransform => 
            new IncludeContextTransform("Content", transformMappings: true)
            .WithMetadata("CopyToOutputDirectory", "PreserveNewest")
            .WithMetadata("CopyToPublishDirectory", "true");
    }
}
