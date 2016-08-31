﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.Build.Construction;
using Microsoft.DotNet.ProjectJsonMigration.Rules;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.Tools.Common;
using Microsoft.DotNet.Tools.Test.Utilities;
using NuGet.Frameworks;
using Xunit;

namespace Microsoft.DotNet.ProjectJsonMigration.Tests
{
    public class GivenAProjectMigrator : TestBase
    {
        [Fact]
        public void It_copies_ProjectDirectory_contents_to_OutputDirectory_when_the_directories_are_different()
        {
            var testProjectDirectory = TestAssetsManager.CreateTestInstance("TestAppSimple", callingMethod: "z")
                .WithLockFiles().Path;
            var outputDirectory = Temp.CreateDirectory().Path;

            var projectDirectoryRelativeFilePaths = EnumerateFilesWithRelativePath(testProjectDirectory);

            var mockProj = ProjectRootElement.Create();
            var testSettings = new MigrationSettings(testProjectDirectory, outputDirectory, "1.0.0", mockProj);

            var projectMigrator = new ProjectMigrator(new FakeEmptyMigrationRule());
            projectMigrator.Migrate(testSettings);

            foreach (var projectDirectoryRelativeFilePath in projectDirectoryRelativeFilePaths)
            {
                File.Exists(Path.Combine(outputDirectory, projectDirectoryRelativeFilePath)).Should().BeTrue();
            }
        }

        [Fact]
        public void It_throws_when_migrating_a_deprecated_projectJson()
        {
            var testProjectDirectory =
                TestAssetsManager.CreateTestInstance("TestLibraryWithDeprecatedProjectFile", callingMethod: "z")
                    .WithLockFiles().Path;

            var mockProj = ProjectRootElement.Create();
            var testSettings = new MigrationSettings(testProjectDirectory, testProjectDirectory, "1.0.0", mockProj);

            var projectMigrator = new ProjectMigrator(new FakeEmptyMigrationRule());
            Action migrateAction = () => projectMigrator.Migrate(testSettings);

            migrateAction.ShouldThrow<Exception>().Where(
                e => e.Message.Contains("MIGRATE1011::Deprecated Project:")
                     && e.Message.Contains("The 'packInclude' option is deprecated. Use 'files' in 'packOptions' instead.")
                     && e.Message.Contains("The 'compilationOptions' option is deprecated. Use 'buildOptions' instead."));
        }

        [Fact]
        public void It_throws_when_migrating_a_non_csharp_app()
        {
            var testProjectDirectory =
                TestAssetsManager.CreateTestInstance("FSharpTestProjects/TestApp", callingMethod: "z")
                    .WithLockFiles().Path;

            var mockProj = ProjectRootElement.Create();
            var testSettings = new MigrationSettings(testProjectDirectory, testProjectDirectory, "1.0.0", mockProj);

            var projectMigrator = new ProjectMigrator(new FakeEmptyMigrationRule());
            Action migrateAction = () => projectMigrator.Migrate(testSettings);

            migrateAction.ShouldThrow<Exception>().Where(
                e => e.Message.Contains("MIGRATE20013::Non-Csharp App: Cannot migrate project"));
        }

        private IEnumerable<string> EnumerateFilesWithRelativePath(string testProjectDirectory)
        {
            return
                Directory.EnumerateFiles(testProjectDirectory, "*", SearchOption.AllDirectories)
                    .Select(file => PathUtility.GetRelativePath(testProjectDirectory, file));
        }

        private class FakeEmptyMigrationRule : IMigrationRule
        {
            public void Apply(MigrationSettings migrationSettings, MigrationRuleInputs migrationRuleInputs)
            {
                return;
            }
        }
    }
}