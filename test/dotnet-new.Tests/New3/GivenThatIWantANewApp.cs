using Microsoft.DotNet.Tools.Test.Utilities;
using System;
using System.IO;
using Xunit;
using FluentAssertions;

namespace Microsoft.DotNet.New3.Tests
{
    public class GivenThatIWantANewApp : TestBase
    {
        [Fact]
        public void When_dotnet_new_is_invoked_mupliple_times_it_should_fail()
        {
            var rootPath = TestAssetsManager.CreateTestDirectory(identifier: "When_dotnet_new_is_invoked_mupliple_times_it_should_fail").Path;

            new TestCommand("dotnet")
                .Execute($"new3 console -o {rootPath}");

            DateTime expectedState = Directory.GetLastWriteTime(rootPath);

            var result = new TestCommand("dotnet")
                .ExecuteWithCapturedOutput($"new3 console -o {rootPath}");

            DateTime actualState = Directory.GetLastWriteTime(rootPath);

            Assert.Equal(expectedState, actualState);

            result.Should().Fail()
                  .And.HaveStdErr();
        }

        [Fact]
        public void RestoreDoesNotUseAnyCliProducedPackagesOnItsTemplates()
        {
            string[] cSharpTemplates = new[] { "console", "classlib", "mstest", "xunit", "web", "mvc", "webapi" };

            var rootPath = TestAssetsManager.CreateTestDirectory(identifier: "new3_RestoreDoesNotUseAnyCliProducedPackagesOnItsTemplates").Path;
            var packagesDirectory = Path.Combine(rootPath, "packages");

            foreach (string cSharpTemplate in cSharpTemplates)
            {
                var projectFolder = Path.Combine(rootPath, cSharpTemplate);
                Directory.CreateDirectory(projectFolder);
                CreateAndRestoreNewProject(cSharpTemplate, projectFolder, packagesDirectory);
            }

            Directory.EnumerateFiles(packagesDirectory, $"*.nupkg", SearchOption.AllDirectories)
                .Should().NotContain(p => p.Contains("Microsoft.DotNet.Cli.Utils"));
        }

        private void CreateAndRestoreNewProject(
            string projectType,
            string projectFolder,
            string packagesDirectory)
        {
            new TestCommand("dotnet")
                .Execute($"new3 {projectType} -o {projectFolder}")
                .Should().Pass();

            new RestoreCommand()
                .WithWorkingDirectory(projectFolder)
                .Execute($"--packages {packagesDirectory} /p:SkipInvalidConfigurations=true")
                .Should().Pass();
        }
    }
}
