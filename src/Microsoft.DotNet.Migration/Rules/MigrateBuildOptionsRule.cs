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
    public class MigrateBuildOptionsRule : IMigrationRule
    {
        // TODO: Migrate Compile, Embed, CopyToOutput items...
        public void Apply(ProjectContext projectContext, ProjectRootElement csproj, string outputDirectory)
        {
            var propertyGroup = csproj.AddPropertyGroup();

            var compilerOptions = projectContext.ProjectFile.GetCompilerOptions(null, null).EmitEntryPoint ?? false;

            EmitEntryPointTransform.Execute(compilerOptions.EmitEntryPoint, csproj, propertyGroup);
            DefineTransform.Execute(compilerOptions.Defines, csproj, propertyGroup);
            NoWarnTransform.Execute(compilerOptions.SuppressWarnings, csproj, propertyGroup);
            WarningsAsErrorsTransform.Execute(compilerOptions.WarningsAsErrors, csproj, propertyGroup);
            AllowUnsafeTransform.Execute(compilerOptions.AllowUnsafe, csproj, propertyGroup);
            OptimizeTransform.Execute(compilerOptions.Optimize, csproj, propertyGroup);
            PlatformTransform.Execute(compilerOptions.Platform, csproj, propertyGroup);
            LanguageVersionTransform.Execute(compilerOptions.LanguageVersion, csproj, propertyGroup);
            KeyFileTransform.Execute(compilerOptions.KeyFile, csproj, propertyGroup);
            DelaySignTransform.Execute(compilerOptions.DelaySign, csproj, propertyGroup);
            PublicSignTransform.Execute(compilerOptions.PublicSign, csproj, propertyGroup);
            DebugTypeTransform.Execute(compilerOptions.DebugType, csproj, propertyGroup);
            XmlDocTransform.Execute(compilerOptions.GenerateXmlDocumentation, csproj, propertyGroup);
            OutputNameTransform.Execute(compilerOptions.OutputName, csproj, propertyGroup);
        }


        private ITransform EmitEntryPointTransform => new AggregateTransform<bool>(
            new AddPropertyTransform<bool>("OutputType", "Exe", e => e),
            new AddPropertyTransform<bool>("TargetExt", "Dll", e => e),
            new AddPropertyTransform<bool>("OutputType", "Library", e => !e));

        private ITransform DefineTransform => new AddPropertyTransform<IEnumerable<string>>(
            "DefineConstants", 
            defines => string.Join(";", defines), 
            defines => defines.Count() > 0);

        private ITransform NoWarnTransform => new AddPropertyTransform<IEnumerable<string>>(
            "NoWarn", 
            nowarn => string.Join(";", nowarn), 
            nowarn => nowarn.Count() > 0);

        private ITransform WarningsAsErrorsTransform = new AddBoolPropertyTransform("WarningsAsErrors");

        private ITransform AllowUnsafeTransform = new AddBoolPropertyTransform("AllowUnsafeBlocks");

        private ITransform OptimizeTransform = new AddBoolPropertyTransform("Optimize");

        private ITransform PlatformTransform = new AddStringPropertyTransform("PlatformTarget");

        private ITransform LanguageVersionTransform = new AddStringPropertyTransform("LanguageVersion");

        private ITransform KeyFileTransform = new AggregateTransform<string>(
            new AddStringPropertyTransform("AssemblyOriginatorKeyFile"),
            new AddPropertyTransform<string>("SignAssembly", "true", s => !string.IsNullOrEmpty(s)));

        private ITransform DelaySignTransform = new AddBoolPropertyTransform("DelaySign");

        private ITransform PublicSignTransform = new AddBoolPropertyTransform("PublicSign");

        private ITransform DebugTypeTransform = new AddStringPropertyTransform("DebugType");

        private ITransform XmlDocTransform = new AddBoolPropertyTransform("GenerateDocumentationFile");

        private ITransform OutputNameTransform = new AddStringPropertyTransform("AssemblyName");  
    }
}
