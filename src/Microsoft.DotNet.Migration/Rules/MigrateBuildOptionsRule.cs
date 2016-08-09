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
using Microsoft.DotNet.Migration.Transforms;
using Microsoft.DotNet.ProjectModel.Files;

namespace Microsoft.DotNet.Migration.Rules
{
    public class MigrateBuildOptionsRule : IMigrationRule
    {
        private bool _insideConfiguration;

        private ProjectPropertyGroupElement _configurationPropertyGroup;
        private ProjectItemGroupElement _configurationItemGroup;

        public MigrateBuildOptionsRule()
        {
            _insideConfiguration = false;
        }

        public MigrateBuildOptionsRule(ProjectPropertyGroupElement configurationPropertyGroup, ProjectItemGroupElement configurationItemGroup)
        {
            _insideConfiguration = true;
            _configurationPropertyGroup = configurationPropertyGroup;
            _configurationItemGroup = configurationItemGroup;
        }

        public void Apply(ProjectContext projectContext, ProjectRootElement csproj, string outputDirectory)
        {
            // Clean Existing properties from Template, if we're not inside a configuration
            if (!_insideConfiguration)
            {
                CleanExistingElements(csproj);
            }

            // Map Properties
            var propertyGroup = _configurationPropertyGroup ?? csproj.AddPropertyGroup();
            var compilerOptions = projectContext.ProjectFile.GetCompilerOptions(null, null);

            EmitEntryPointTransform.Execute(compilerOptions.EmitEntryPoint ?? false, csproj, propertyGroup);
            DefineTransform.Execute(compilerOptions.Defines, csproj, propertyGroup);
            NoWarnTransform.Execute(compilerOptions.SuppressWarnings, csproj, propertyGroup);
            WarningsAsErrorsTransform.Execute(compilerOptions.WarningsAsErrors ?? false, csproj, propertyGroup);
            AllowUnsafeTransform.Execute(compilerOptions.AllowUnsafe ?? false, csproj, propertyGroup);
            OptimizeTransform.Execute(compilerOptions.Optimize ?? false, csproj, propertyGroup);
            PlatformTransform.Execute(compilerOptions.Platform, csproj, propertyGroup);
            LanguageVersionTransform.Execute(compilerOptions.LanguageVersion, csproj, propertyGroup);
            KeyFileTransform.Execute(compilerOptions.KeyFile, csproj, propertyGroup);
            DelaySignTransform.Execute(compilerOptions.DelaySign ?? false, csproj, propertyGroup);
            PublicSignTransform.Execute(compilerOptions.PublicSign ?? false, csproj, propertyGroup);
            DebugTypeTransform.Execute(compilerOptions.DebugType, csproj, propertyGroup);
            XmlDocTransform.Execute(compilerOptions.GenerateXmlDocumentation ?? false, csproj, propertyGroup);
            OutputNameTransform.Execute(compilerOptions.OutputName, csproj, propertyGroup);
            PreserveCompilationContextTransform.Execute(compilerOptions.PreserveCompilationContext ?? false, csproj, propertyGroup);

            // Map Compile, Embed, CopyToOutput
            var itemGroup = _configurationItemGroup ?? csproj.AddItemGroup();

            CompileFilesTransform.Execute(compilerOptions.CompileInclude, csproj, itemGroup);
            EmbedFilesTransform.Execute(compilerOptions.EmbedInclude, csproj, itemGroup);
            CopyToOutputFilesTransform.Execute(compilerOptions.CopyToOutputInclude, csproj, itemGroup);
        }

        private void CleanExistingElements(ProjectRootElement csproj)
        {
            var existingPropertiesToRemove = new string[] { "OutputType", "TargetExt" };
            var existingItemsToRemove = new string[] { "Compile" };

            foreach (var propertyName in existingPropertiesToRemove)
            {
                var properties = csproj.Properties.Where(p => p.Name == propertyName);

                foreach (var property in properties)
                {
                    property.Parent.RemoveChild(property);
                }
            }

            foreach (var itemName in existingItemsToRemove)
            {
                var items = csproj.Items.Where(p => p.Name == itemName);

                foreach (var item in items)
                {
                    item.Parent.RemoveChild(item);
                }
            }
        }

        private ITransform<bool> EmitEntryPointTransform => new AggregateTransform<bool>(
            new AddPropertyTransform<bool>("OutputType", "Exe", emitEntryPoint => emitEntryPoint),
            new AddPropertyTransform<bool>("TargetExt", "Dll", emitEntryPoint => emitEntryPoint),
            new AddPropertyTransform<bool>("OutputType", "Library", emitEntryPoint => !emitEntryPoint));

        private ITransform<IEnumerable<string>> DefineTransform => new AddPropertyTransform<IEnumerable<string>>(
            "DefineConstants", 
            defines => string.Join(";", defines), 
            defines => defines.Count() > 0);

        private ITransform<IEnumerable<string>> NoWarnTransform => new AddPropertyTransform<IEnumerable<string>>(
            "NoWarn", 
            nowarn => string.Join(";", nowarn), 
            nowarn => nowarn.Count() > 0);

        private ITransform<bool> PreserveCompilationContextTransform => new AddBoolPropertyTransform("PreserveCompilationContext");

        private ITransform<bool> WarningsAsErrorsTransform => new AddBoolPropertyTransform("WarningsAsErrors");

        private ITransform<bool> AllowUnsafeTransform => new AddBoolPropertyTransform("AllowUnsafeBlocks");

        private ITransform<bool> OptimizeTransform => new AddBoolPropertyTransform("Optimize");

        private ITransform<string> PlatformTransform => new AddStringPropertyTransform("PlatformTarget");

        private ITransform<string> LanguageVersionTransform => new AddStringPropertyTransform("LanguageVersion");

        private ITransform<string> KeyFileTransform => new AggregateTransform<string>(
            new AddStringPropertyTransform("AssemblyOriginatorKeyFile"),
            new AddPropertyTransform<string>("SignAssembly", "true", s => !string.IsNullOrEmpty(s)));

        private ITransform<bool> DelaySignTransform => new AddBoolPropertyTransform("DelaySign");

        private ITransform<bool> PublicSignTransform => new AddBoolPropertyTransform("PublicSign");

        private ITransform<string> DebugTypeTransform => new AddStringPropertyTransform("DebugType");

        private ITransform<bool> XmlDocTransform => new AddBoolPropertyTransform("GenerateDocumentationFile");

        private ITransform<string> OutputNameTransform => new AddStringPropertyTransform("AssemblyName");

        private ITransform<IncludeContext> CompileFilesTransform => new IncludeContextTransform("Compile", transformMappings: false);

        private ITransform<IncludeContext> EmbedFilesTransform => new IncludeContextTransform("EmbeddedResource", transformMappings: false);

        private ITransform<IncludeContext> CopyToOutputFilesTransform => 
            new IncludeContextTransform("Content", transformMappings: true)
            .WithMetadata("CopyToOutputDirectory", "PreserveNewest");
    }
}
