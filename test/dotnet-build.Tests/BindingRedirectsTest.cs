using System;
using System.IO;
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
            string root = testInstance.TestRoot;

            var buildOutputPath = Path.Combine(testInstance.TestRoot, "artifacts");
            var buildCommand = new BuildCommand(
                projectPath: GetProjectPath(testInstance.TestRoot),
                output: buildOutputPath,
                framework: framework);
            buildCommand.Execute().Should().Pass();

            var appConfigFileName = buildCommand.GetOutputExecutableName() + ".config";
            new DirectoryInfo(buildOutputPath).Should().HaveFile(appConfigFileName);

            new AppConfig(Path.Combine(buildOutputPath, appConfigFileName))
                .Should().BindRedirect("dotnet-base", "b570ffd6752684c6")
                .From("1.0.0.0")
                .To("2.0.0.0");
        }

        private string GetProjectPath(string projectDir)
        {
            return Path.Combine(projectDir, "project.json");
        }
    }
}
