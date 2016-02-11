using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Tools.Test.Utilities;

namespace Microsoft.DotNet.Tools.Builder.Tests
{
    public class BindingRedirectsTest : TestBase
    {
        private readonly string _testProjectsRoot;

        public BindingRedirectsTest()
        {
            _testProjectsRoot = Path.Combine(AppContext.BaseDirectory, "TestAssets", "TestProjects");
        }

        [WindowsOnlyFact]
        public void TestAppGeneratesCorrectBindings()
        {
            var framework = "net461";
            TempDirectory root = Temp.CreateDirectory();

            TempDirectory projectsRoot = root.CreateDirectory("TestBindingsRedirectsGeneration2");
            TempDirectory referencedLibDir = projectsRoot.CreateDirectory("ReferencedLibrary");
            TempDirectory testLibRootDir = projectsRoot.CreateDirectory("TestLibrary");
            TempDirectory referencedLibDirV1 = testLibRootDir.CreateDirectory("ReferencedLibrary");
            TempDirectory testLibDir = testLibRootDir.CreateDirectory("TestLibrary");
            TempDirectory testAppDir = projectsRoot.CreateDirectory("TestApp");

            string tbrg2Path = Path.Combine(_testProjectsRoot, "TestBindingsRedirectsGeneration2");
            string testLibRootPath = Path.Combine(tbrg2Path, "TestLibrary");

            CopyProjectToTempDir(Path.Combine(tbrg2Path, "ReferencedLibrary"), referencedLibDir);
            CopyProjectToTempDir(Path.Combine(testLibRootPath, "TestLibrary"), testLibDir);
            CopyProjectToTempDir(Path.Combine(testLibRootPath, "ReferencedLibrary"), referencedLibDirV1);
            CopyProjectToTempDir(Path.Combine(tbrg2Path, "TestApp"), testAppDir);
            CopyProjectToTempDir(tbrg2Path, projectsRoot);

            new BuildCommand(GetProjectPath(referencedLibDir)).Execute();
            new BuildCommand(GetProjectPath(referencedLibDirV1),
                output: Path.Combine(referencedLibDirV1.Path, "artifacts"),
                framework: framework).Execute();
            new BuildCommand(GetProjectPath(testLibDir)).Execute();

            new PackCommand(GetProjectPath(referencedLibDir),
                output: Path.Combine(projectsRoot.Path, @"packages"))
                .Execute();
            new PackCommand(GetProjectPath(testLibDir),
                output: Path.Combine(projectsRoot.Path, @"packages"))
                .Execute();

            new RestoreCommand().Execute(GetProjectPath(testAppDir)).Should().Pass();

            var buildOutputPath = Path.Combine(testAppDir.Path, "artifacts");
            var appBuildCommand = new BuildCommand(GetProjectPath(testAppDir),
                output: buildOutputPath,
                framework: framework);
            appBuildCommand.Execute().Should().Pass();

            var appConfigFileName = appBuildCommand.GetOutputExecutableName() + ".config";
            new DirectoryInfo(buildOutputPath)
                .Should().HaveFile(appConfigFileName);

            new AppConfig(Path.Combine(buildOutputPath, appConfigFileName))
                .Should().BindRedirect("ReferencedLibrary", "b570ffd6752684c6")
                .From("1.0.0.0")
                .To("2.0.0.0");
        }

        private void CopyProjectToTempDir(string projectDir, TempDirectory tempDir)
        {
            // copy all the files to temp dir
            foreach (var file in Directory.EnumerateFiles(projectDir))
            {
                tempDir.CopyFile(file);
            }
        }

        private string GetProjectPath(TempDirectory projectDir)
        {
            return Path.Combine(projectDir.Path, "project.json");
        }
    }
}
