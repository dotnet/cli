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
using Microsoft.DotNet.ProjectJsonMigration.Transforms;
using Microsoft.DotNet.ProjectModel.Files;

namespace Microsoft.DotNet.ProjectJsonMigration.Rules
{
    public class MigrateRootOptionsRule : IMigrationRule
    {
        public void Apply(ProjectContext projectContext, ProjectRootElement csproj, string outputDirectory)
        {
            var propertyGroup = csproj.AddPropertyGroup();

            DescriptionTransform.Execute(projectContext.ProjectFile.Description, csproj, propertyGroup);
            CopyrightTransform.Execute(projectContext.ProjectFile.Copyright, csproj, propertyGroup);
            TitleTransform.Execute(projectContext.ProjectFile.Title, csproj, propertyGroup);
            LanguageTransform.Execute(projectContext.ProjectFile.Language, csproj, propertyGroup);
        }
        
        private ITransform<string> DescriptionTransform => new AddStringPropertyTransform("Description");

        private ITransform<string> CopyrightTransform => new AddStringPropertyTransform("Copyright");

        private ITransform<string> TitleTransform => new AddStringPropertyTransform("AssemblyTitle");

        private ITransform<string> LanguageTransform => new AddStringPropertyTransform("NeutralLanguage");
    }
}
