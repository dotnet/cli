// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.DotNet.ProjectModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using System.Linq;
using FluentAssertions;

namespace Microsoft.DotNet.ProjectModel.Tests
{
    public class GivenThatIWantToFilesMatchedWithPatternsInJson
    {
        private const string ProjectName = "some project name";
        private readonly string ProjectFilePath = AppContext.BaseDirectory;

        [Fact]
        public void PackInclude_is_empty_when_it_is_not_set_in_the_ProjectJson()
        {
            var json = new JObject();
            var project = GetProject(json);

            project.Files.PackInclude.Should().BeEmpty();
        }

        [Fact]
        public void It_sets_PackInclude_when_packInclude_is_set_in_the_ProjectJson()
        {
            const string somePackTarget = "some pack target";
            const string somePackValue = "some pack value";

            var json = new JObject();
            var packIncludeJson = new JObject();
            json.Add("packInclude", packIncludeJson);

            packIncludeJson.Add(somePackTarget, somePackValue);

            var project = GetProject(json);

            var packInclude = project.Files.PackInclude.FirstOrDefault();

            packInclude.Target.Should().Be(somePackTarget);
            packInclude.SourceGlobs.Should().Contain(somePackValue);
        }

        [Fact]
        public void It_parses_the_builtIns_successfully()
        {
            var json = new JObject();
            json.Add("excludeBuiltIn", "*.fs");
            json.Add("exclude", "bin/");
            json.Add("contentBuiltIn", "*.jpg");
            json.Add("compileBuiltIn", "*.cs");
            json.Add("resourceBuiltIn", "*.resx");
            json.Add("publishExclude", "*.pdb");
            Action action = () => GetProject(json);

            action.ShouldNotThrow();
        }

        [Fact]
        public void It_parses_the_patterns_successfully()
        {
            var json = new JObject();
            json.Add("excludeBuiltIn", "*.fs");
            json.Add("exclude", "bin/");
            json.Add("contentBuiltIn", "*.jpg");
            json.Add("compileBuiltIn", "*.cs");
            json.Add("resourceBuiltIn", "*.resx");
            json.Add("publishExclude", "*.pdb");
            json.Add("shared", "*.bin");
            json.Add("resource", "../../**/*.resx");
            json.Add("preprocess", "*.cs");
            json.Add("compile", "*");
            json.Add("content", "*.jpge");
            Action action = () => GetProject(json);

            action.ShouldNotThrow();
        }

        [Fact]
        public void It_parses_namedResources_successfully()
        {
            var json = new JObject();
            var namedResources= new JObject();
            json.Add("namedResource", namedResources);

            namedResources.Add("System.Strings", "System/Strings.resx");

            var project = GetProject(json);

            project.Files.ResourceFiles.Keys.Should().Contain(
                Path.GetFullPath(Path.Combine(Path.GetDirectoryName(ProjectFilePath), "System", "Strings.resx")));
            project.Files.ResourceFiles.Values.Should().Contain("System.Strings");
        }

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
