using System;
using System.IO;
using FluentAssertions;
using Microsoft.DotNet.Tools.Test.Utilities;

namespace Microsoft.DotNet.Tools.Builder.Tests
{
    public class BindingRedirectsTest : TestBase
    {
        private readonly string _testProjectsRoot;
        private readonly string _projectName = "TestBindingsRedirectsGeneration2";

        public BindingRedirectsTest()
        {
            _testProjectsRoot = Path.Combine(AppContext.BaseDirectory, "TestAssets", "TestProjects");
        }

        [WindowsOnlyFact]
        public void TestAppGeneratesCorrectBindings()
        {
            var testInstance = TestAssetsManager.CreateTestInstance(_projectName).WithLockFiles();
            string framework = "net461";
            string configuration = "Release";

            string root = testInstance.TestRoot;

            var buildOutputRoot = Path.Combine(testInstance.TestRoot, "bin", configuration, framework);
            var buildCommand = new BuildCommand(
                projectPath: GetProjectPath(testInstance.TestRoot),
                configuration: configuration,
                framework: framework);
            var result = buildCommand.ExecuteWithCapturedOutput();
            result.Should().Pass();

            var appConfigFileName = buildCommand.GetOutputExecutableName() + ".config";
            var appConfigFiles = new DirectoryInfo(buildOutputRoot).GetFiles(appConfigFileName, SearchOption.AllDirectories);
            appConfigFiles.Should().HaveCount(c => c > 0);

            foreach (var f in appConfigFiles)
            {
                new AppConfig(f.FullName).Should().BindRedirect("dotnet-base", "b570ffd6752684c6")
                    .From("1.0.0.0")
                    .To("2.0.0.0");
            }
        }

        private string GetProjectPath(string projectDir)
        {
            return Path.Combine(projectDir, "project.json");
        }
    }
}
