﻿using Microsoft.Build.Construction;
using Microsoft.DotNet.ProjectJsonMigration;
using Microsoft.DotNet.Internal.ProjectModel;
using Microsoft.DotNet.Tools.Test.Utilities;
using NuGet.Frameworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Microsoft.DotNet.ProjectJsonMigration.Rules;

namespace Microsoft.DotNet.ProjectJsonMigration.Tests
{
    public class GivenThatIWantToMigrateTFMs : TestBase
    {
        [Fact(Skip="Emitting this until x-targetting full support is in")]
        public void Migrating_netcoreapp_project_Does_not_populate_TargetFrameworkIdentifier_and_TargetFrameworkVersion()
        {
            var testDirectory = Temp.CreateDirectory().Path;
            var testPJ = new ProjectJsonBuilder(TestAssetsManager)
                .FromTestAssetBase("TestAppWithRuntimeOptions")
                .WithCustomProperty("buildOptions", new Dictionary<string, string>
                {
                    { "emitEntryPoint", "false" }
                })
                .SaveToDisk(testDirectory);

            var projectContext = ProjectContext.Create(testDirectory, FrameworkConstants.CommonFrameworks.NetCoreApp10);
            var mockProj = ProjectRootElement.Create();

            var migrationSettings = new MigrationSettings(testDirectory, testDirectory, mockProj);
            var migrationInputs = new MigrationRuleInputs(
                new[] { projectContext }, 
                mockProj, 
                mockProj.AddItemGroup(),
                mockProj.AddPropertyGroup());

            new MigrateTFMRule().Apply(migrationSettings, migrationInputs);

            mockProj.Properties.Count(p => p.Name == "TargetFrameworkIdentifier").Should().Be(0);
            mockProj.Properties.Count(p => p.Name == "TargetFrameworkVersion").Should().Be(0);
        }

        [Fact]
        public void Migrating_MultiTFM_project_Populates_TargetFrameworks_with_short_tfms()
        {
            var testDirectory = Temp.CreateDirectory().Path;
            var testPJ = new ProjectJsonBuilder(TestAssetsManager)
                .FromTestAssetBase("TestLibraryWithMultipleFrameworks")
                .SaveToDisk(testDirectory);

            var projectContexts = ProjectContext.CreateContextForEachFramework(testDirectory);
            var mockProj = ProjectRootElement.Create();

            var migrationSettings = new MigrationSettings(testDirectory, testDirectory, mockProj);
            var migrationInputs = new MigrationRuleInputs(
                projectContexts, 
                mockProj, 
                mockProj.AddItemGroup(), 
                mockProj.AddPropertyGroup());

            new MigrateTFMRule().Apply(migrationSettings, migrationInputs);

            mockProj.Properties.Count(p => p.Name == "TargetFrameworks").Should().Be(1);
            mockProj.Properties.First(p => p.Name == "TargetFrameworks")
                .Value.Should().Be("net20;net35;net40;net461;netstandard1.5");
        }

        [Fact]
        public void Migrating_desktop_TFMs_adds_RuntimeIdentifiers()
        {
            var testDirectory = Temp.CreateDirectory().Path;
            var testPJ = new ProjectJsonBuilder(TestAssetsManager)
                .FromTestAssetBase("TestLibraryWithMultipleFrameworks")
                .SaveToDisk(testDirectory);

            var projectContexts = ProjectContext.CreateContextForEachFramework(testDirectory);
            var mockProj = ProjectRootElement.Create();

            var migrationSettings = new MigrationSettings(testDirectory, testDirectory, mockProj);
            var migrationInputs = new MigrationRuleInputs(
                projectContexts, 
                mockProj, 
                mockProj.AddItemGroup(), 
                mockProj.AddPropertyGroup());

            new MigrateTFMRule().Apply(migrationSettings, migrationInputs);

            mockProj.Properties.Count(p => p.Name == "RuntimeIdentifiers").Should().Be(1);
            mockProj.Properties.First(p => p.Name == "RuntimeIdentifiers")
                .Value.Should().Be("win7-x64;win7-x86;osx.10.10-x64;osx.10.11-x64;ubuntu.14.04-x64;ubuntu.16.04-x64;centos.7-x64;rhel.7.2-x64;debian.8-x64;fedora.23-x64;opensuse.13.2-x64");
        }

        [Fact]
        public void Migrating_Single_TFM_project_Populates_TargetFramework()
        {
            var testDirectory = Temp.CreateDirectory().Path;
            var testPJ = new ProjectJsonBuilder(TestAssetsManager)
                .FromTestAssetBase("TestAppWithRuntimeOptions")
                .WithCustomProperty("buildOptions", new Dictionary<string, string>
                {
                    { "emitEntryPoint", "false" }
                })
                .SaveToDisk(testDirectory);

            var projectContexts = ProjectContext.CreateContextForEachFramework(testDirectory);
            var mockProj = ProjectRootElement.Create();

            // Run BuildOptionsRule
            var migrationSettings = new MigrationSettings(testDirectory, testDirectory, mockProj);
            var migrationInputs = new MigrationRuleInputs(
                projectContexts, 
                mockProj, 
                mockProj.AddItemGroup(), 
                mockProj.AddPropertyGroup());

            new MigrateTFMRule().Apply(migrationSettings, migrationInputs);
            Console.WriteLine(mockProj.RawXml);

            mockProj.Properties.Count(p => p.Name == "TargetFramework").Should().Be(1);
        }
    }
}
