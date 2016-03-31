// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;
using Microsoft.DotNet.ProjectModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using FluentAssertions;
using NuGet.Versioning;
using System.Linq;

namespace Microsoft.DotNet.ProjectModel.Tests
{
    public class GivenThatIWantToLoadAProjectJsonFile
    {
        private const string ProjectName = "some project name";
        private const string SomeLanguageVersion = "some language version";
        private const string SomeOutputName = "some output name";
        private const string SomePlatform = "some platform";
        private const string SomeKeyFile = "some key file";
        private const string SomeDebugType = "some debug type";
        private readonly string ProjectFilePath = AppContext.BaseDirectory;

        private Project _emptyProject;
        private readonly string[] _someDefines = new[] {"DEFINE1", "DEFINE2"};
        private readonly string[] _noWarnings = new[] {"warn1", "warn2"};
        private readonly string[] _someAdditionalArguments = new[] {"additional argument 1", "additional argument 2"};
        private readonly JObject _jsonCompilationOptions;
        private CommonCompilerOptions commonCompilerOptions;

        public GivenThatIWantToLoadAProjectJsonFile()
        {
            var json = new JObject();
            _emptyProject = GetProject(json);
            _jsonCompilationOptions = new JObject();

            _jsonCompilationOptions.Add("define", new JArray(_someDefines));
            _jsonCompilationOptions.Add("nowarn", new JArray(_noWarnings));
            _jsonCompilationOptions.Add("additionalArguments", new JArray(_someAdditionalArguments));
            _jsonCompilationOptions.Add("languageVersion", SomeLanguageVersion);
            _jsonCompilationOptions.Add("outputName", SomeOutputName);
            _jsonCompilationOptions.Add("platform", SomePlatform);
            _jsonCompilationOptions.Add("keyFile", SomeKeyFile);
            _jsonCompilationOptions.Add("debugType", SomeDebugType);
            _jsonCompilationOptions.Add("allowUnsafe", true);
            _jsonCompilationOptions.Add("warningsAsErrors", true);
            _jsonCompilationOptions.Add("optimize", true);
            _jsonCompilationOptions.Add("delaySign", true);
            _jsonCompilationOptions.Add("publicSign", true);
            _jsonCompilationOptions.Add("emitEntryPoint", true);
            _jsonCompilationOptions.Add("xmlDoc", true);
            _jsonCompilationOptions.Add("preserveCompilationContext", true);

            commonCompilerOptions = new CommonCompilerOptions
            {
                Defines = _someDefines,
                SuppressWarnings = _noWarnings,
                AdditionalArguments = _someAdditionalArguments,
                LanguageVersion = SomeLanguageVersion,
                OutputName = SomeOutputName,
                Platform = SomePlatform,
                KeyFile = SomeKeyFile,
                DebugType = SomeDebugType,
                AllowUnsafe = true,
                WarningsAsErrors = true,
                Optimize = true,
                DelaySign = true,
                PublicSign = true,
                EmitEntryPoint = true,
                GenerateXmlDocumentation = true,
                PreserveCompilationContext = true
            };
        }

        [Fact]
        public void It_does_not_throw_when_the_project_json_is_empty()
        {
            var json = new JObject();
            Action action = () => GetProject(json);

            action.ShouldNotThrow<Exception>();
        }

        [Fact]
        public void It_sets_Name_to_the_passed_ProjectName_if_one_is_not_set_in_the_ProjectJson()
        {
            _emptyProject.Name.Should().Be(ProjectName);
        }

        [Fact]
        public void It_sets_Name_to_the_Name_in_the_ProjectJson_when_one_is_set()
        {
            const string nameInprojectJson = "some name in the project.json";
            var json = new JObject();
            json.Add("name", nameInprojectJson);
            var project = GetProject(json);

            project.Name.Should().Be(nameInprojectJson);
        }

        [Fact]
        public void It_sets_the_project_file_path()
        {
            _emptyProject.ProjectFilePath.Should().Be(ProjectFilePath);
        }

        [Fact]
        public void It_sets_the_version_to_one_when_it_is_not_set()
        {
            _emptyProject.Version.Should().Be(new NuGetVersion("1.0.0"));
        }

        [Fact]
        public void It_sets_the_version_to_the_one_in_the_ProjectJson_when_one_is_set()
        {
            var json = new JObject();
            json.Add("version", "1.1");
            var project = GetProject(json);

            project.Version.Should().Be(new NuGetVersion("1.1"));
        }

        [Fact]
        public void It_sets_AssemblyFileVersion_to_the_ProjectJson_version_when_AssemblyFileVersion_is_no_passed_in_the_settings()
        {
            var json = new JObject();
            json.Add("version", "1.1");
            var project = GetProject(json);

            project.AssemblyFileVersion.Should().Be(new NuGetVersion("1.1").Version);
        }

        [Fact]
        public void It_sets_AssemblyFileVersion_Revision_to_the_AssemblyFileVersion_passed_in_the_settings_and_everything_else_to_the_projectJson_Version()
        {
            const int revision = 1;
            var json = new JObject();
            json.Add("version", "1.1");
            var project = GetProject(json, new ProjectReaderSettings { AssemblyFileVersion = revision.ToString() });

            var version = new NuGetVersion("1.1").Version;
            project.AssemblyFileVersion.Should().Be(
                new Version(version.Major, version.Minor, version.Build, revision));
        }

        [Fact]
        public void It_throws_a_FormatException_when_AssemblyFileVersion_passed_in_the_settings_is_invalid()
        {
            var json = new JObject();
            json.Add("version", "1.1");
            Action action = () =>
                GetProject(json, new ProjectReaderSettings { AssemblyFileVersion = "not a revision" });

            action.ShouldThrow<FormatException>().WithMessage("The assembly file version is invalid: not a revision");
        }

        [Fact]
        public void It_leaves_marketing_information_empty_when_it_is_not_set_in_the_ProjectJson()
        {
            _emptyProject.Description.Should().BeNull();
            _emptyProject.Summary.Should().BeNull();
            _emptyProject.Copyright.Should().BeNull();
            _emptyProject.Title.Should().BeNull();
            _emptyProject.EntryPoint.Should().BeNull();
            _emptyProject.ProjectUrl.Should().BeNull();
            _emptyProject.LicenseUrl.Should().BeNull();
            _emptyProject.IconUrl.Should().BeNull();
            _emptyProject.Authors.Should().BeEmpty();
            _emptyProject.Owners.Should().BeEmpty();
            _emptyProject.Tags.Should().BeEmpty();
            _emptyProject.Language.Should().BeNull();
            _emptyProject.ReleaseNotes.Should().BeNull();
        }

        [Fact]
        public void It_sets_the_marketing_information_when_it_is_set_in_the_ProjectJson()
        {
            const string someDescription = "some description";
            const string someSummary = "some summary";
            const string someCopyright = "some copyright";
            const string someTitle = "some title";
            const string someEntryPoint = "some entry point";
            const string someProjectUrl = "some project url";
            const string someLicenseUrl = "some license url";
            const string someIconUrl = "some icon url";
            const string someLanguage = "some language";
            const string someReleaseNotes = "someReleaseNotes";
            var authors = new [] {"some author", "and another author"};
            var owners = new[] {"some owner", "a second owner"};
            var tags = new[] {"tag1", "tag2"};

            var json = new JObject();
            json.Add("description", someDescription);
            json.Add("summary", someSummary);
            json.Add("copyright", someCopyright);
            json.Add("title", someTitle);
            json.Add("entryPoint", someEntryPoint);
            json.Add("projectUrl", someProjectUrl);
            json.Add("licenseUrl", someLicenseUrl);
            json.Add("iconUrl", someIconUrl);
            json.Add("authors", new JArray(authors));
            json.Add("owners", new JArray(owners));
            json.Add("tags", new JArray(tags));
            json.Add("language", someLanguage);
            json.Add("releaseNotes", someReleaseNotes);
            var project = GetProject(json);

            project.Description.Should().Be(someDescription);
            project.Summary.Should().Be(someSummary);
            project.Copyright.Should().Be(someCopyright);
            project.Title.Should().Be(someTitle);
            project.EntryPoint.Should().Be(someEntryPoint);
            project.ProjectUrl.Should().Be(someProjectUrl);
            project.LicenseUrl.Should().Be(someLicenseUrl);
            project.IconUrl.Should().Be(someIconUrl);
            project.Authors.Should().Contain(authors);
            project.Owners.Should().Contain(owners);
            project.Tags.Should().Contain(tags);
            project.Language.Should().Be(someLanguage);
            project.ReleaseNotes.Should().Be(someReleaseNotes);
        }

        [Fact]
        public void It_sets_the_compilerName_to_csc_when_one_is_not_set_in_the_ProjectJson()
        {
            _emptyProject.CompilerName.Should().Be("csc");
        }

        [Fact]
        public void It_sets_the_compilerName_to_the_one_in_the_ProjectJson()
        {
            const string compilerName = "a compiler different from csc";
            var json = new JObject();
            json.Add("compilerName", compilerName);
            var project = GetProject(json);

            project.CompilerName.Should().Be(compilerName);
        }

        [Fact]
        public void It_leaves_testRunner_null_when_one_is_not_set_in_the_ProjectJson()
        {
            _emptyProject.TestRunner.Should().BeNull();
        }

        [Fact]
        public void It_sets_testRunner_to_the_one_in_the_ProjectJson()
        {
            const string someTestRunner = "some test runner";
            var json = new JObject();
            json.Add("testRunner", someTestRunner);
            var project = GetProject(json);

            project.TestRunner.Should().Be(someTestRunner);
        }

        [Fact]
        public void It_sets_requireLicenseAcceptance_to_false_when_one_is_not_set_in_the_ProjectJson()
        {
            _emptyProject.RequireLicenseAcceptance.Should().BeFalse();
        }

        [Fact]
        public void It_sets_requireLicenseAcceptance_to_the_one_in_the_ProjectJson()
        {
            var json = new JObject();
            json.Add("requireLicenseAcceptance", true);
            var project = GetProject(json);

            project.RequireLicenseAcceptance.Should().BeTrue();
        }

        [Fact]
        public void It_sets_embedInteropTypes_to_false_when_one_is_not_set_in_the_ProjectJson()
        {
            _emptyProject.EmbedInteropTypes.Should().BeFalse();
        }

        [Fact]
        public void It_sets_embedInteropTypes_to_the_one_in_the_ProjectJson()
        {
            var json = new JObject();
            json.Add("embedInteropTypes", true);
            var project = GetProject(json);

            project.EmbedInteropTypes.Should().BeTrue();
        }

        [Fact]
        public void It_does_not_add_commands_when_commands_is_not_set_in_the_ProjectJson()
        {
            _emptyProject.Commands.Should().BeEmpty();
        }

        [Fact]
        public void It_does_not_add_commands_when_commands_is_not_a_JsonObject()
        {
            var json = new JObject();
            json.Add("commands", true);
            var project = GetProject(json);

            project.Commands.Should().BeEmpty();
        }

        [Fact]
        public void It_does_not_add_the_commands_when_its_value_is_not_a_string()
        {
            var json = new JObject();
            var commands = new JObject();
            json.Add("commands", commands);

            commands.Add("commandKey1", "commandValue1");
            commands.Add("commandKey2", true);

            var project = GetProject(json);

            project.Commands.Count.Should().Be(1);
            project.Commands.First().Key.Should().Be("commandKey1");
            project.Commands.First().Value.Should().Be("commandValue1");
        }

        [Fact]
        public void It_does_not_add_scripts_when_scripts_is_not_set_in_the_ProjectJson()
        {
            _emptyProject.Scripts.Should().BeEmpty();
        }

        [Fact]
        public void It_does_not_add_scripts_when_scripts_is_not_a_JsonObject()
        {
            var json = new JObject();
            json.Add("scripts", true);
            var project = GetProject(json);

            project.Scripts.Should().BeEmpty();
        }

        [Fact]
        public void It_does_not_add_the_scripts_when_its_value_is_neither_a_string_nor_an_array_of_strings()
        {
            var scriptArrayValues = new [] {"scriptValue2", "scriptValue3"};

            var json = new JObject();
            var scripts = new JObject();
            json.Add("scripts", scripts);

            scripts.Add("scriptKey1", "scriptValue1");
            scripts.Add("scriptKey3", new JArray(scriptArrayValues));

            var project = GetProject(json);

            project.Scripts.Count.Should().Be(2);
            project.Scripts.First().Key.Should().Be("scriptKey1");
            project.Scripts.First().Value.Should().Contain("scriptValue1");
            project.Scripts["scriptKey3"].Should().Contain(scriptArrayValues);
        }

        [Fact]
        public void It_throws_when_the_value_of_a_script_is_neither_a_string_nor_array_of_strings()
        {
            var json = new JObject();
            var scripts = new JObject();
            json.Add("scripts", scripts);

            scripts.Add("scriptKey2", true);

            Action action = () => GetProject(json);

            action.ShouldThrow<FileFormatException>()
                .WithMessage("The value of a script in project.json can only be a string or an array of strings");
        }

        [Fact]
        public void It_uses_an_empty_compiler_options_when_one_is_not_set_in_the_ProjectJson()
        {
            _emptyProject.GetCompilerOptions(null, null).Should().Be(new CommonCompilerOptions
            {
                OutputName = ProjectName
            });
        }

        [Fact]
        public void It_sets_analyzerOptions_when_it_is_set_in_the_compilationOptions_in_the_ProjectJson()
        {
            var json = new JObject();
            var compilationOptions = new JObject();
            json.Add("compilationOptions", compilationOptions);

            var analyzerOptions = new JObject();
            compilationOptions.Add("analyzerOptions", analyzerOptions);

            analyzerOptions.Add("languageId", "C#");

            var project = GetProject(json);
            project.AnalyzerOptions.LanguageId.Should().Be("C#");
        }

        [Fact]
        public void It_throws_when_the_analyzerOptions_languageId_is_no_a_string()
        {
            var json = new JObject();
            var compilationOptions = new JObject();
            json.Add("compilationOptions", compilationOptions);

            var analyzerOptions = new JObject();
            compilationOptions.Add("analyzerOptions", analyzerOptions);

            analyzerOptions.Add("languageId", true);

            Action action = () => GetProject(json);

            action.ShouldThrow<FileFormatException>().WithMessage("The analyzer languageId must be a string");
        }

        [Fact]
        public void It_throws_when_the_analyzerOptions_has_no_languageId()
        {
            var json = new JObject();
            var compilationOptions = new JObject();
            json.Add("compilationOptions", compilationOptions);

            var analyzerOptions = new JObject();
            compilationOptions.Add("analyzerOptions", analyzerOptions);

            analyzerOptions.Add("differentFromLanguageId", "C#");

            Action action = () => GetProject(json);

            action.ShouldThrow<FileFormatException>()
                .WithMessage("Unrecognized analyzerOption key: differentFromLanguageId");
        }

        [Fact]
        public void It_sets_compilationOptions_when_it_is_set_in_the_compilationOptions_in_the_ProjectJson()
        {
            var json = new JObject();
            json.Add("compilationOptions", _jsonCompilationOptions);

            var project = GetProject(json);

            project.GetCompilerOptions(null, null).Should().Be(commonCompilerOptions);
        }

        [Fact]
        public void It_merges_configuration_sections_set_in_the_ProjectJson()
        {
            var json = new JObject();
            var configurations = new JObject();
            json.Add("compilationOptions", _jsonCompilationOptions);
            json.Add("configurations", configurations);

            _jsonCompilationOptions["allowUnsafe"] = null;

            var someConfiguration = new JObject();
            configurations.Add("some configuration", someConfiguration);
            var someConfigurationCompilationOptions = new JObject();
            someConfiguration.Add("compilationOptions", someConfigurationCompilationOptions);
            someConfigurationCompilationOptions.Add("allowUnsafe", false);

            var project = GetProject(json);

            commonCompilerOptions.AllowUnsafe = false;

            project.GetCompilerOptions(null, "some configuration").Should().Be(commonCompilerOptions);
        }

        [Fact]
        public void It_does_not_set_rawRuntimeOptions_when_it_is_not_set_in_the_ProjectJson()
        {
            _emptyProject.RawRuntimeOptions.Should().BeNull();
        }

        [Fact]
        public void It_throws_when_runtimeOptions_is_not_a_Json_object()
        {
            var json = new JObject();
            json.Add("runtimeOptions", "not a json object");

            Action action = () => GetProject(json);

            action.ShouldThrow<FileFormatException>().WithMessage("The runtimeOptions must be an object");
        }

        [Fact]
        public void It_sets_the_rawRuntimeOptions_serialized_when_it_is_set_in_the_ProjectJson()
        {
            var configProperties = new JObject();
            configProperties.Add("System.GC.Server", true);
            var runtimeOptions = new JObject();
            runtimeOptions.Add("configProperties", configProperties);
            var json = new JObject();
            json.Add("runtimeOptions", runtimeOptions);

            var project = GetProject(json);

            project.RawRuntimeOptions.Should().Be(runtimeOptions.ToString());
        }

        //TODO: dependencies and tools

        //TODO: test frameworks section
//        [Fact]
//        public void It_merges_framework_sections_with_compilerOptions_when_it_is_set_in_the_ProjectJson()
//        {
//            var json = new JObject();
//            var frameworks = new JObject();
//            json.Add("compilationOptions", _jsonCompilationOptions);
//            json.Add("frameworks", frameworks);
//
//            _jsonCompilationOptions["allowUnsafe"] = null;
//
//            var someFramework = new JObject();
//            frameworks.Add("netstandard1.5", someFramework);
//            var someConfigurationCompilationOptions = new JObject();
//            someFramework.Add("compilationOptions", someConfigurationCompilationOptions);
//            someConfigurationCompilationOptions.Add("allowUnsafe", false);
//
//            var project = GetProject(json);
//
//            commonCompilerOptions.AllowUnsafe = false;
//
//            project.GetCompilerOptions(null, "some configuration").Should().Be(commonCompilerOptions);
//        }

        public Project GetProject(JObject json, ProjectReaderSettings settings = null)
        {
            using (var stream = new MemoryStream())
            {
                using (var sw = new StreamWriter(stream, Encoding.UTF8, 256, true))
                {
                    using (var writer = new JsonTextWriter(sw))
                    {
                        writer.Formatting = Formatting.Indented;
                        json.WriteTo(writer);
                    }

                    stream.Position = 0;
                    var projectReader = new ProjectReader();
                    return projectReader.ReadProject(
                        stream,
                        ProjectName,
                        ProjectFilePath,
                        new List<DiagnosticMessage>(),
                        settings);
                }
            }
        }
    }
}
