using FluentAssertions;
using Microsoft.DotNet.TestFramework;
using Microsoft.DotNet.Tools.Common;
using Microsoft.DotNet.Tools.Test.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Microsoft.DotNet.Migration.Tests
{
    public class GivenThatAnAppWasMigrated : TestBase
    {
        [Theory]
        [InlineData("PJTestAppSimple")]
        [InlineData("PJTestAppWithLibrary")]
        public void WhenMigrationSucceedsThenProjectJsonArtifactsGetMovedToBackup(string testProjectName)
        {
            var testRoot = TestAssets.Get(testProjectName)
                .CreateInstance()
                .WithSourceFiles()
                .Root;

            var backupRoot = testRoot.GetDirectory("backup");

            var migratableArtifacts = GetProjectJsonArtifacts(testRoot);
            
            new MigrateCommand()
                .WithWorkingDirectory(testRoot)
                .Execute()
                .Should().Pass();

            var backupArtifacts = GetProjectJsonArtifacts(backupRoot);

            backupArtifacts.Should().Equal(migratableArtifacts, "Because all of and only these artifacts should have been moved");

            testRoot.Should().NotHaveFiles(backupArtifacts.Keys, "Because all of these artifacts should have been moved.");

            backupRoot.Should().HaveTextFiles(backupArtifacts, "Because all of these artifacts should have been moved.");
        }

        [Theory]
        [InlineData("TestAppWithLibraryAndMissingP2P")]
        public void WhenMigrationFailsThenProjectJsonArtifactsDoNotGetMovedToBackup(string testProjectName)
        {
            var testRoot = TestAssets.Get(TestAssetKinds.NonRestoredTestProjects, testProjectName)
                .CreateInstance()
                .WithSourceFiles()
                .Root;

            var backupRoot = testRoot.GetDirectory("backup");

            var migratableArtifacts = GetProjectJsonArtifacts(testRoot);

            new MigrateCommand()
                .WithWorkingDirectory(testRoot)
                .Execute()
                .Should().Fail();
            
            backupRoot.Should().NotExist("Because migration failed and therefore no backup is needed.");

            testRoot.Should().HaveTextFiles(migratableArtifacts, "Because migration failed so nothing was moved to backup.");
        }

        [Theory]
        [InlineData("PJTestAppSimple")]
        public void When_skipbackup_specified_Then_project_json_artifacts_do_not_get_moved_to_backup(string testProjectName)
        {
            var testRoot = TestAssets.Get(testProjectName)
                .CreateInstance()
                .WithSourceFiles()
                .Root;

            var backupRoot = testRoot.GetDirectory("backup");

            var migratableArtifacts = GetProjectJsonArtifacts(testRoot);

            new MigrateCommand()
                .WithWorkingDirectory(testRoot)
                .Execute("--skip-backup")
                .Should().Pass();
            
            backupRoot.Should().NotExist("Because --skip-backup was specified.");

            testRoot.Should().HaveTextFiles(migratableArtifacts, "Because --skip-backup was specified.");
        }

        private Dictionary<string, string> GetProjectJsonArtifacts(DirectoryInfo root)
        {
            var catalog = new Dictionary<string, string>();
            
            var patterns = new [] { "global.json", "project.json", "project.lock.json", "*.xproj", "*.xproj.user" };

            foreach (var pattern in patterns)
            {
                AddArtifactsToCatalog(catalog, root, pattern);
            }
            
            return catalog;
        }

        private void AddArtifactsToCatalog(Dictionary<string, string> catalog, DirectoryInfo root, string pattern)
        {
            var files = root.GetFiles(pattern, SearchOption.AllDirectories);

            foreach (var file in files)
            {
                catalog.Add(PathUtility.GetRelativePath(root, file), File.ReadAllText(file.FullName));
            }
        }
    }
}
