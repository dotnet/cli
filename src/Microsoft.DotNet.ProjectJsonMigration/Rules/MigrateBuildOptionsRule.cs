// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Construction;
using Microsoft.DotNet.ProjectJsonMigration.Transforms;
using Microsoft.DotNet.Internal.ProjectModel;
using Microsoft.DotNet.Internal.ProjectModel.Files;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.ProjectJsonMigration.Rules
{
    internal class MigrateBuildOptionsRule : IMigrationRule
    {
        private AddPropertyTransform<CommonCompilerOptions>[] EmitEntryPointTransforms
            => new []
            {
                new AddPropertyTransform<CommonCompilerOptions>("OutputType", "Exe",
                    compilerOptions => compilerOptions.EmitEntryPoint != null && compilerOptions.EmitEntryPoint.Value),
                new AddPropertyTransform<CommonCompilerOptions>("OutputType", "Library",
                    compilerOptions => compilerOptions.EmitEntryPoint != null && !compilerOptions.EmitEntryPoint.Value)
            };

        private AddPropertyTransform<CommonCompilerOptions>[] KeyFileTransforms
            => new []
            {
                new AddPropertyTransform<CommonCompilerOptions>("AssemblyOriginatorKeyFile",
                    compilerOptions => compilerOptions.KeyFile,
                    compilerOptions => !string.IsNullOrEmpty(compilerOptions.KeyFile)),
                new AddPropertyTransform<CommonCompilerOptions>("SignAssembly",
                    "true",
                    compilerOptions => !string.IsNullOrEmpty(compilerOptions.KeyFile)),
                new AddPropertyTransform<CommonCompilerOptions>("PublicSign", 
                    "true",
                    compilerOptions => !string.IsNullOrEmpty(compilerOptions.KeyFile) && (compilerOptions.PublicSign == null))
                    .WithMSBuildCondition(" '$(OS)' != 'Windows_NT' ")
            };

        private AddPropertyTransform<CommonCompilerOptions> DefineTransform => new AddPropertyTransform<CommonCompilerOptions>(
            "DefineConstants", 
            compilerOptions => "$(DefineConstants);" + string.Join(";", compilerOptions.Defines),
            compilerOptions => compilerOptions.Defines != null && compilerOptions.Defines.Any());

        private AddPropertyTransform<CommonCompilerOptions> NoWarnTransform => new AddPropertyTransform<CommonCompilerOptions>(
            "NoWarn",
            compilerOptions => "$(NoWarn);" + string.Join(";", compilerOptions.SuppressWarnings),
            compilerOptions => compilerOptions.SuppressWarnings != null && compilerOptions.SuppressWarnings.Any());

        private AddPropertyTransform<CommonCompilerOptions> PreserveCompilationContextTransform =>
            new AddPropertyTransform<CommonCompilerOptions>("PreserveCompilationContext",
                compilerOptions => compilerOptions.PreserveCompilationContext.ToString().ToLower(),
                compilerOptions => compilerOptions.PreserveCompilationContext != null && compilerOptions.PreserveCompilationContext.Value);

        private AddPropertyTransform<CommonCompilerOptions> WarningsAsErrorsTransform =>
            new AddPropertyTransform<CommonCompilerOptions>("WarningsAsErrors",
                compilerOptions => compilerOptions.WarningsAsErrors.ToString().ToLower(),
                compilerOptions => compilerOptions.WarningsAsErrors != null && compilerOptions.WarningsAsErrors.Value);

        private AddPropertyTransform<CommonCompilerOptions> AllowUnsafeTransform =>
            new AddPropertyTransform<CommonCompilerOptions>("AllowUnsafeBlocks",
                compilerOptions => compilerOptions.AllowUnsafe.ToString().ToLower(),
                compilerOptions => compilerOptions.AllowUnsafe != null && compilerOptions.AllowUnsafe.Value);

        private AddPropertyTransform<CommonCompilerOptions> OptimizeTransform =>
            new AddPropertyTransform<CommonCompilerOptions>("Optimize",
                compilerOptions => compilerOptions.Optimize.ToString().ToLower(),
                compilerOptions => compilerOptions.Optimize != null && compilerOptions.Optimize.Value);

        private AddPropertyTransform<CommonCompilerOptions> PlatformTransform =>
            new AddPropertyTransform<CommonCompilerOptions>("PlatformTarget",
                compilerOptions => compilerOptions.Platform,
                compilerOptions => !string.IsNullOrEmpty(compilerOptions.Platform));

        private AddPropertyTransform<CommonCompilerOptions> LanguageVersionTransform =>
            new AddPropertyTransform<CommonCompilerOptions>("LangVersion",
                compilerOptions => FormatLanguageVersion(compilerOptions.LanguageVersion),
                compilerOptions => !string.IsNullOrEmpty(compilerOptions.LanguageVersion));

        private AddPropertyTransform<CommonCompilerOptions> DelaySignTransform =>
            new AddPropertyTransform<CommonCompilerOptions>("DelaySign",
                compilerOptions => compilerOptions.DelaySign.ToString().ToLower(),
                compilerOptions => compilerOptions.DelaySign != null && compilerOptions.DelaySign.Value);

        private AddPropertyTransform<CommonCompilerOptions> PublicSignTransform =>
            new AddPropertyTransform<CommonCompilerOptions>("PublicSign",
                compilerOptions => compilerOptions.PublicSign.ToString().ToLower(),
                compilerOptions => compilerOptions.PublicSign != null && compilerOptions.PublicSign.Value);

        private AddPropertyTransform<CommonCompilerOptions> DebugTypeTransform =>
            new AddPropertyTransform<CommonCompilerOptions>("DebugType",
                compilerOptions => compilerOptions.DebugType,
                compilerOptions => !string.IsNullOrEmpty(compilerOptions.DebugType));

        private AddPropertyTransform<CommonCompilerOptions> XmlDocTransform =>
            new AddPropertyTransform<CommonCompilerOptions>("GenerateDocumentationFile",
                compilerOptions => compilerOptions.GenerateXmlDocumentation.ToString().ToLower(),
                compilerOptions => compilerOptions.GenerateXmlDocumentation != null && compilerOptions.GenerateXmlDocumentation.Value);    

        private AddPropertyTransform<CommonCompilerOptions> AssemblyNameTransform =>
            new AddPropertyTransform<CommonCompilerOptions>("AssemblyName",
                compilerOptions => compilerOptions.OutputName,
                compilerOptions => compilerOptions.OutputName != null);

        private Func<string, AddPropertyTransform<CommonCompilerOptions>> PackageIdTransformExecute =>
            (projectFolderName) =>
                new AddPropertyTransform<CommonCompilerOptions>("PackageId",
                    compilerOptions => projectFolderName,
                    compilerOptions => compilerOptions.OutputName != null);

        private string[] _compilePatternsToExclude = new string[] {
            "**/*.cs"
        };

        private static bool IsPlainFileName(string fileName)
        {
            return !fileName.Contains('/') && !fileName.Contains('\\');
        }

        private bool CompileFilesExcludeRule(string pattern)
        {
            return _compilePatternsToExclude.Contains(pattern.Replace('\\', '/'))
                || IsPlainFileName(pattern);
        }

        private IncludeContextTransform CompileFilesTransform =>
            new IncludeContextTransform(
                "Compile",
                transformMappings: false,
                excludePatternsRule: CompileFilesExcludeRule,
                condition: ic => ic != null,
                emitBuiltInIncludes: false);

        private RemoveContextTransform RemoveCompileFilesTransform =>
            new RemoveContextTransform(
                "Compile",
                condition: ic => ic != null);

        private IncludeContextTransform EmbedFilesTransform =>
            new IncludeContextTransform("EmbeddedResource", transformMappings: false, condition: ic => ic != null);

        private RemoveContextTransform RemoveEmbedFilesTransform =>
            new RemoveContextTransform("EmbeddedResource", condition: ic => ic != null);

        private IncludeContextTransform CopyToOutputFilesTransform =>
            new IncludeContextTransform("Content", transformMappings: true)
                .WithMetadata("CopyToOutputDirectory", "PreserveNewest");

        private IncludeContextTransform CopyToOutputFilesTransformForWeb =>
            new UpdateContextTransform("Content", transformMappings: true)
                .WithMetadata("CopyToOutputDirectory", "PreserveNewest");

        private AddPropertyTransform<Project> GenerateRuntimeConfigurationFilesTransform => 
            new AddPropertyTransform<Project>(
                "GenerateRuntimeConfigurationFiles", 
                project => "true",
                project => project.GetProjectType() == ProjectType.Test);

        private AddItemTransform<ProjectContext> AppConfigTransform =>
            new AddItemTransform<ProjectContext>(
                "None",
                projectContext => "App.config",
                projectContext => string.Empty,
                projectContext => File.Exists(Path.Combine(projectContext.ProjectDirectory, "App.config")));

        private Func<CommonCompilerOptions, string, ProjectType, IEnumerable<ProjectItemElement>> CompileFilesTransformExecute =>
            (compilerOptions, projectDirectory, projectType) =>
                    CompileFilesTransform.Transform(compilerOptions.CompileInclude);

        private Func<CommonCompilerOptions, string, ProjectType, IEnumerable<ProjectItemElement>> RemoveCompileFilesTransformExecute =>
            (compilerOptions, projectDirectory, projectType) =>
                    RemoveCompileFilesTransform.Transform(compilerOptions.CompileInclude);

        private Func<CommonCompilerOptions, string, ProjectType, IEnumerable<ProjectItemElement>> EmbedFilesTransformExecute =>
            (compilerOptions, projectDirectory, projectType) =>
                    EmbedFilesTransform.Transform(GetEmbedIncludeContext(compilerOptions));

        private Func<CommonCompilerOptions, string, ProjectType, IEnumerable<ProjectItemElement>> RemoveEmbedFilesTransformExecute =>
            (compilerOptions, projectDirectory, projectType) =>
                    RemoveEmbedFilesTransform.Transform(GetEmbedIncludeContext(compilerOptions));

        private Func<CommonCompilerOptions, string, ProjectType, IEnumerable<ProjectItemElement>> CopyToOutputFilesTransformExecute =>
            (compilerOptions, projectDirectory, projectType) =>
            {
                var copyToOutputFilesTransform =
                    projectType == ProjectType.Web ?
                    CopyToOutputFilesTransformForWeb :
                    CopyToOutputFilesTransform;

                return copyToOutputFilesTransform.Transform(GetCopyToOutputIncludeContext(compilerOptions, projectDirectory));
            };

        private readonly string[] DefaultEmptyExcludeOption = new string[0];

        private readonly ProjectPropertyGroupElement _configurationPropertyGroup;
        private readonly ProjectItemGroupElement _configurationItemGroup;
        private readonly CommonCompilerOptions _configurationBuildOptions;

        private List<AddPropertyTransform<CommonCompilerOptions>> _propertyTransforms;
        private List<Func<CommonCompilerOptions, string, ProjectType, IEnumerable<ProjectItemElement>>> _removeContextTransformExecutes;
        private List<Func<CommonCompilerOptions, string, ProjectType, IEnumerable<ProjectItemElement>>> _includeContextTransformExecutes;

        private readonly ITransformApplicator _transformApplicator;

        public MigrateBuildOptionsRule(ITransformApplicator transformApplicator = null)
        {
            _transformApplicator = transformApplicator ?? new TransformApplicator();
            ConstructTransformLists();
        }

        public MigrateBuildOptionsRule(
            CommonCompilerOptions configurationBuildOptions,
            ProjectPropertyGroupElement configurationPropertyGroup,
            ProjectItemGroupElement configurationItemGroup,
            ITransformApplicator transformApplicator = null)
        {
            _configurationBuildOptions = configurationBuildOptions;
            _configurationPropertyGroup = configurationPropertyGroup;
            _configurationItemGroup = configurationItemGroup;
            _transformApplicator = transformApplicator ?? new TransformApplicator();

            ConstructTransformLists();
        }

        private void ConstructTransformLists()
        {
            _propertyTransforms = new List<AddPropertyTransform<CommonCompilerOptions>>()
            {
                DefineTransform,
                NoWarnTransform,
                WarningsAsErrorsTransform,
                AllowUnsafeTransform,
                OptimizeTransform,
                PlatformTransform,
                LanguageVersionTransform,
                DelaySignTransform,
                PublicSignTransform,
                DebugTypeTransform,
                XmlDocTransform,
                PreserveCompilationContextTransform,
                AssemblyNameTransform
            };

            _propertyTransforms.AddRange(EmitEntryPointTransforms);
            _propertyTransforms.AddRange(KeyFileTransforms);

            _removeContextTransformExecutes =
                new List<Func<CommonCompilerOptions, string, ProjectType, IEnumerable<ProjectItemElement>>>()
                {
                    RemoveCompileFilesTransformExecute,
                    RemoveEmbedFilesTransformExecute
                };

            _includeContextTransformExecutes =
                new List<Func<CommonCompilerOptions, string, ProjectType, IEnumerable<ProjectItemElement>>>()
                {
                    CompileFilesTransformExecute,
                    EmbedFilesTransformExecute,
                    CopyToOutputFilesTransformExecute
                };
        }

        public void Apply(MigrationSettings migrationSettings, MigrationRuleInputs migrationRuleInputs)
        {
            var csproj = migrationRuleInputs.OutputMSBuildProject;
            var projectContext = migrationRuleInputs.DefaultProjectContext;

            var propertyGroup = _configurationPropertyGroup ?? migrationRuleInputs.CommonPropertyGroup;
            var itemGroup = _configurationItemGroup ?? migrationRuleInputs.CommonItemGroup;

            var compilerOptions = projectContext.ProjectFile.GetCompilerOptions(null, null);

            var projectDirectoryName = new DirectoryInfo(migrationSettings.ProjectDirectory).Name;
            _propertyTransforms.Add(PackageIdTransformExecute(projectDirectoryName));

            var project = migrationRuleInputs.DefaultProjectContext.ProjectFile;
            var projectType = project.GetProjectType();

            // If we're in a configuration, we need to be careful not to overwrite values from BuildOptions
            // without a configuration
            if (_configurationBuildOptions == null)
            {
                CleanExistingProperties(csproj);

                PerformPropertyAndItemMappings(
                    compilerOptions,
                    propertyGroup,
                    itemGroup,
                    _transformApplicator,
                    migrationSettings.ProjectDirectory,
                    projectType,
                    csproj);
            }
            else
            {
                PerformConfigurationPropertyAndItemMappings(
                    compilerOptions,
                    _configurationBuildOptions,
                    propertyGroup,
                    itemGroup,
                    _transformApplicator,
                    migrationSettings.ProjectDirectory,
                    projectType,
                    csproj);
            }

            var transformOutput = GenerateRuntimeConfigurationFilesTransform.Transform(
                migrationRuleInputs.DefaultProjectContext.ProjectFile);
            _transformApplicator.Execute(transformOutput, propertyGroup, mergeExisting: true);

            var appConfigTransformOutput = AppConfigTransform.Transform(migrationRuleInputs.DefaultProjectContext);
            _transformApplicator.Execute(appConfigTransformOutput, itemGroup, mergeExisting: true);
        }

        private void PerformConfigurationPropertyAndItemMappings(
            CommonCompilerOptions compilerOptions,
            CommonCompilerOptions configurationCompilerOptions,
            ProjectPropertyGroupElement propertyGroup,
            ProjectItemGroupElement itemGroup,
            ITransformApplicator transformApplicator,
            string projectDirectory,
            ProjectType projectType,
            ProjectRootElement csproj)
        {
            foreach (var transform in _propertyTransforms)
            {
                var nonConfigurationOutput = transform.Transform(compilerOptions);
                var configurationOutput = transform.Transform(configurationCompilerOptions);

                if (!PropertiesAreEqual(nonConfigurationOutput, configurationOutput))
                {
                    transformApplicator.Execute(configurationOutput, propertyGroup, mergeExisting: true);
                }
            }

            foreach (var removeContextTransformExecutes in _removeContextTransformExecutes)
            {
                var nonConfigurationOutput =
                    removeContextTransformExecutes(compilerOptions, projectDirectory, projectType);
                var configurationOutput =
                    removeContextTransformExecutes(configurationCompilerOptions, projectDirectory, projectType);

                if (configurationOutput != null)
                {
                    transformApplicator.Execute(configurationOutput, itemGroup, mergeExisting: true);
                }
            }

            foreach (var includeContextTransformExecute in _includeContextTransformExecutes)
            {
                var nonConfigurationOutput =
                    includeContextTransformExecute(compilerOptions, projectDirectory, projectType);
                var configurationOutput =
                    includeContextTransformExecute(configurationCompilerOptions, projectDirectory, projectType);

                if (configurationOutput != null)
                {
                    transformApplicator.Execute(configurationOutput, itemGroup, mergeExisting: true);
                }
            }
        }

        private bool PropertiesAreEqual(ProjectPropertyElement nonConfigurationOutput, ProjectPropertyElement configurationOutput)
        {
            if (configurationOutput != null && nonConfigurationOutput != null)
            {
                return string.Equals(nonConfigurationOutput.Value, configurationOutput.Value, StringComparison.Ordinal);
            }

            return configurationOutput == nonConfigurationOutput;
        }

        private void PerformPropertyAndItemMappings(
            CommonCompilerOptions compilerOptions,
            ProjectPropertyGroupElement propertyGroup, 
            ProjectItemGroupElement itemGroup,
            ITransformApplicator transformApplicator,
            string projectDirectory,
            ProjectType projectType,
            ProjectRootElement csproj)
        {
            foreach (var transform in _propertyTransforms)
            {
                transformApplicator.Execute(transform.Transform(compilerOptions), propertyGroup, mergeExisting: true);
            }

            foreach (var removeContextTransformExecutes in _removeContextTransformExecutes)
            {
                var transform = removeContextTransformExecutes(compilerOptions, projectDirectory, projectType);

                if (transform != null)
                {
                    transformApplicator.Execute(
                        transform,
                        itemGroup,
                        mergeExisting: true);
                }
            }

            foreach (var includeContextTransformExecute in _includeContextTransformExecutes)
            {
                var transform = includeContextTransformExecute(compilerOptions, projectDirectory, projectType);

                if (transform != null)
                {
                    transformApplicator.Execute(
                        transform,
                        itemGroup,
                        mergeExisting: true);
                }
            }
        }

        private void CleanExistingProperties(ProjectRootElement csproj)
        {
            var existingPropertiesToRemove = new [] {"OutputType", "TargetExt"};

            foreach (var propertyName in existingPropertiesToRemove)
            {
                var properties = csproj.Properties.Where(p => p.Name == propertyName);

                foreach (var property in properties)
                {
                    property.Parent.RemoveChild(property);
                }
            }
        }

        private IncludeContext GetEmbedIncludeContext(CommonCompilerOptions compilerOptions)
        {
            var embedIncludeContext = compilerOptions.EmbedInclude;

            embedIncludeContext?.BuiltInsExclude.Add("@(EmbeddedResource)");

            return embedIncludeContext;
        }

        private IncludeContext GetCopyToOutputIncludeContext(CommonCompilerOptions compilerOptions, string projectDirectory)
        {
            // Defaults from src/Microsoft.DotNet.ProjectModel/ProjectReader.cs #608
            return compilerOptions.CopyToOutputInclude ??
                new IncludeContext(
                    projectDirectory,
                    "copyToOutput",
                    new JObject(),
                    null,
                    null);
        }

        private string FormatLanguageVersion(string langVersion)
        {
            if (langVersion.StartsWith("csharp", StringComparison.OrdinalIgnoreCase))
            {
                return langVersion.Substring("csharp".Length);
            }

            return langVersion;
        }
    }
}
