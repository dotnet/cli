using System;
using System.IO;
using FluentAssertions;
using Microsoft.DotNet.TestFramework;
using Microsoft.DotNet.Tools.Test.Utilities;

namespace Microsoft.DotNet.Tools.Builder.Tests
{
    public class BindingRedirectsTest : TestBase
    {
        private readonly string _testProjectsRoot;
        private readonly string _projectName = "TestBindingRedirectGeneration2";

        public BindingRedirectsTest()
        {
            _testProjectsRoot = Path.Combine(AppContext.BaseDirectory, "TestAssets", "TestProjects");
        }

        [WindowsOnlyFact]
        public void TestAppGeneratesCorrectBindings()
        {
            var assetsManager = GetAssetsManager();
            var testInstance = assetsManager.CreateTestInstance(_projectName).WithLockFiles();

            string framework = "net461";
            string configuration = "Release";

            string appRoot = Path.Combine(testInstance.TestRoot, "test-app");
            string packagesPath = Path.Combine(testInstance.TestRoot, "packages");

            new PackCommand(GetProjectPath(Path.Combine(testInstance.TestRoot, "dotnet-base")), output: packagesPath).Execute();
            new PackCommand(GetProjectPath(Path.Combine(testInstance.TestRoot, "dotnet-dep", "dotnet-base")), output: packagesPath).Execute();
            new PackCommand(GetProjectPath(Path.Combine(testInstance.TestRoot, "dotnet-dep", "dotnet-dep")), output: packagesPath).Execute();

            var restore = new RestoreCommand().Execute($"{GetProjectPath(appRoot)} --fallbacksource {packagesPath}");

            var buildOutputRoot = Path.Combine(appRoot, "bin", configuration, framework);
            var buildCommand = new BuildCommand(
                projectPath: GetProjectPath(appRoot),
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

        private TestAssetsManager GetAssetsManager()
        {
            string assetsRoot = Path.Combine(RepoRoot, "TestAssets", "TestProjectsNoBuild");
            return new TestAssetsManager(assetsRoot);
        }
    }
}
