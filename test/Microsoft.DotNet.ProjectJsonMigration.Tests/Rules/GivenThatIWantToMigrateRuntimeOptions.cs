// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;
using Microsoft.DotNet.ProjectJsonMigration;
using System;
using System.IO;
using Microsoft.Build.Construction;
using Microsoft.DotNet.Internal.ProjectModel;
using NuGet.Frameworks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Microsoft.DotNet.ProjectJsonMigration.Rules;

namespace Microsoft.DotNet.ProjectJsonMigration.Tests
{
    public class GivenThatIWantToMigrateRuntimeOptions : TestBase
    {
        private static readonly string s_runtimeConfigFileName = "runtimeconfig.template.json";

        [Fact]
        public void RuntimeOptions_are_copied_from_projectJson_to_runtimeconfig_template_json_file()
        {
            var testInstance = TestAssets.Get("PJTestAppWithRuntimeOptions").CreateInstance().WithSourceFiles();
            var projectDir = testInstance.Root.FullName;
            var projectPath = Path.Combine(testInstance.Root.FullName, "project.json");

            var project = JObject.Parse(File.ReadAllText(projectPath));
            var rawRuntimeOptions = (JObject)project.GetValue("runtimeOptions");

            var projectContext = ProjectContext.Create(projectDir, FrameworkConstants.CommonFrameworks.NetCoreApp10);

            var testSettings = MigrationSettings.CreateMigrationSettingsTestHook(projectDir, projectDir, default(ProjectRootElement));
            var testInputs = new MigrationRuleInputs(new[] { projectContext }, null, null, null);
            new MigrateRuntimeOptionsRule().Apply(testSettings, testInputs);

            var migratedRuntimeOptionsPath = Path.Combine(projectDir, s_runtimeConfigFileName);

            File.Exists(migratedRuntimeOptionsPath).Should().BeTrue();

            var migratedRuntimeOptionsContent = JObject.Parse(File.ReadAllText(migratedRuntimeOptionsPath));
            JToken.DeepEquals(rawRuntimeOptions, migratedRuntimeOptionsContent).Should().BeTrue();
        }

        [Fact]
        public void Migrating_ProjectJson_with_no_RuntimeOptions_produces_no_runtimeconfig_template_json_file()
        {
            var projectDir = TestAssets.Get("PJTestAppSimple")
                .CreateInstance()
                .WithSourceFiles()
                .Root.FullName;

            var projectContext = ProjectContext.Create(projectDir, FrameworkConstants.CommonFrameworks.NetCoreApp10);

            var testSettings = MigrationSettings.CreateMigrationSettingsTestHook(projectDir, projectDir, default(ProjectRootElement));
            var testInputs = new MigrationRuleInputs(new[] { projectContext }, null, null, null);
            new MigrateRuntimeOptionsRule().Apply(testSettings, testInputs);

            var migratedRuntimeOptionsPath = Path.Combine(projectDir, s_runtimeConfigFileName);

            File.Exists(migratedRuntimeOptionsPath).Should().BeFalse();
        }
    }
}
