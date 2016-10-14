using Microsoft.DotNet.Tools.Test.Utilities;
using System.Collections.Generic;
using System.IO;
using Xunit;
using FluentAssertions;

namespace Microsoft.DotNet.Migration.Tests
{
    public class GivenThatAnAppWasMigrated : TestBase
    {
        [Fact]
        public void When_the_app_has_a_single_project_Then_the_projectfiles_are_moved_to_backup()
        {
            var testRoot = TestAssetsManager.CreateTestInstance("ProjectJsonConsoleTemplate").Path;

            var backupRoot = Path.Combine(testRoot, ".backup");

            var projectArtifacts = GetProjectJsonArtifacts(testRoot);

            new RestoreCommand()
                .WithWorkingDirectory(testRoot)
                .Execute();
            
            new MigrateCommand()
                .WithWorkingDirectory(testRoot)
                .Execute();

            var backupArtifacts = GetProjectJsonArtifacts(backupRoot);

            //projectArtifacts.Should().
        }

        [Fact]
        public void When_the_app_has_a_globajson_Then_the_projectfiles_are_moved_to_backup()
        {

        }

        private Dictionary<string, string> GetProjectJsonArtifacts(string rootPath)
        {
            return new Dictionary<string, string>();
        }
    }
}
