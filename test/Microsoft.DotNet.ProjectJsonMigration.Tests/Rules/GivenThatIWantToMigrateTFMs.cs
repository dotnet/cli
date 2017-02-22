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
        public void MigratingNetcoreappProjectDoesNotPopulateTargetFrameworkIdentifierAndTargetFrameworkVersion()
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

            var migrationSettings = MigrationSettings.CreateMigrationSettingsTestHook(testDirectory, testDirectory, mockProj);
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
        public void MigratingMultiTFMProjectPopulatesTargetFrameworksWithShortTfms()
        {
            var testDirectory = Temp.CreateDirectory().Path;
            var testPJ = new ProjectJsonBuilder(TestAssetsManager)
                .FromTestAssetBase("TestLibraryWithMultipleFrameworks")
                .SaveToDisk(testDirectory);

            var projectContexts = ProjectContext.CreateContextForEachFramework(testDirectory);
            var mockProj = ProjectRootElement.Create();

            var migrationSettings = MigrationSettings.CreateMigrationSettingsTestHook(testDirectory, testDirectory, mockProj);
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
        public void MigratingCoreAndDesktopTFMsDoesNoAddRuntimeIdentifiersOrRuntimeIdentifierWhenTheProjectDoesNothaveAnyAlready()
        {
            var testDirectory = Temp.CreateDirectory().Path;
            var testPJ = new ProjectJsonBuilder(TestAssetsManager)
                .FromTestAssetBase("PJAppWithMultipleFrameworks")
                .SaveToDisk(testDirectory);

            var projectContexts = ProjectContext.CreateContextForEachFramework(testDirectory);
            var mockProj = ProjectRootElement.Create();

            var migrationSettings = MigrationSettings.CreateMigrationSettingsTestHook(testDirectory, testDirectory, mockProj);
            var migrationInputs = new MigrationRuleInputs(
                projectContexts, 
                mockProj, 
                mockProj.AddItemGroup(), 
                mockProj.AddPropertyGroup());

            new MigrateTFMRule().Apply(migrationSettings, migrationInputs);

            mockProj.Properties.Count(p => p.Name == "RuntimeIdentifiers").Should().Be(0);
            mockProj.Properties.Count(p => p.Name == "RuntimeIdentifier").Should().Be(0);
        }

        [Fact]
        public void MigrateTFMRuleDoesNotAddRuntimesWhenMigratingDesktopTFMsWithRuntimesAlready()
        {
            var testDirectory = Temp.CreateDirectory().Path;
            var testPJ = new ProjectJsonBuilder(TestAssetsManager)
                .FromTestAssetBase("TestAppWithMultipleFrameworksAndRuntimes")
                .SaveToDisk(testDirectory);

            var projectContexts = ProjectContext.CreateContextForEachFramework(testDirectory);
            var mockProj = ProjectRootElement.Create();

            var migrationSettings =
                MigrationSettings.CreateMigrationSettingsTestHook(testDirectory, testDirectory, mockProj);
            var migrationInputs = new MigrationRuleInputs(
                projectContexts, 
                mockProj, 
                mockProj.AddItemGroup(), 
                mockProj.AddPropertyGroup());

            new MigrateTFMRule().Apply(migrationSettings, migrationInputs);

            mockProj.Properties.Count(p => p.Name == "RuntimeIdentifiers").Should().Be(0);
        }

        [Fact]
        public void MigratingProjectWithFullFrameworkTFMsDoesNotAddRuntimeIdentifiersOrRuntimeIdentiferWhenNoRuntimesExistAlready()
        {
            var testDirectory = Temp.CreateDirectory().Path;
            var testPJ = new ProjectJsonBuilder(TestAssetsManager)
                .FromTestAssetBase("TestAppWithMultipleFullFrameworksOnly")
                .SaveToDisk(testDirectory);

            var projectContexts = ProjectContext.CreateContextForEachFramework(testDirectory);
            var mockProj = ProjectRootElement.Create();

            var migrationSettings =
                MigrationSettings.CreateMigrationSettingsTestHook(testDirectory, testDirectory, mockProj);
            var migrationInputs = new MigrationRuleInputs(
                projectContexts, 
                mockProj, 
                mockProj.AddItemGroup(), 
                mockProj.AddPropertyGroup());

            new MigrateTFMRule().Apply(migrationSettings, migrationInputs);

            mockProj.Properties.Count(p => p.Name == "RuntimeIdentifiers").Should().Be(0);
            mockProj.Properties.Where(p => p.Name == "RuntimeIdentifier").Should().HaveCount(0);
        }

        [Fact]
        public void MigratingSingleTFMProjectPopulatesTargetFramework()
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
            var migrationSettings = MigrationSettings.CreateMigrationSettingsTestHook(testDirectory, testDirectory, mockProj);
            var migrationInputs = new MigrationRuleInputs(
                projectContexts, 
                mockProj, 
                mockProj.AddItemGroup(), 
                mockProj.AddPropertyGroup());

            new MigrateTFMRule().Apply(migrationSettings, migrationInputs);
            Console.WriteLine(mockProj.RawXml);

            mockProj.Properties.Count(p => p.Name == "TargetFramework").Should().Be(1);
        }

        [Fact]
        public void MigratingLibWithMultipleTFMsDoesNotAddRuntimes()
        {
            var testDirectory = Temp.CreateDirectory().Path;
            var testPJ = new ProjectJsonBuilder(TestAssetsManager)
                .FromTestAssetBase("PJLibWithMultipleFrameworks")
                .SaveToDisk(testDirectory);

            var projectContexts = ProjectContext.CreateContextForEachFramework(testDirectory);
            var mockProj = ProjectRootElement.Create();

            var migrationSettings =
                MigrationSettings.CreateMigrationSettingsTestHook(testDirectory, testDirectory, mockProj);
            var migrationInputs = new MigrationRuleInputs(
                projectContexts,
                mockProj,
                mockProj.AddItemGroup(),
                mockProj.AddPropertyGroup());

            new MigrateTFMRule().Apply(migrationSettings, migrationInputs);

            var reason = "Should not add runtime identifiers for libraries";
            mockProj.Properties.Count(p => p.Name == "RuntimeIdentifiers").Should().Be(0, reason);
            mockProj.Properties.Count(p => p.Name == "RuntimeIdentifier").Should().Be(0, reason);
        }
    }
}
