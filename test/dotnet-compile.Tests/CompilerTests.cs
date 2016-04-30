// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.TestFramework;
using Microsoft.DotNet.Tools.Test.Utilities;
using FluentAssertions;
using Xunit;

namespace Microsoft.DotNet.Tools.Compiler.Tests
{
    public class CompilerTests : TestBase
    {
        private readonly string _testProjectsRoot;

        public CompilerTests()
        {
            _testProjectsRoot = Path.Combine(AppContext.BaseDirectory, "TestAssets", "TestProjects");
        }

        [Fact]
        public void When_xmlDoc_is_true_then_doc_xml_is_generated()
        {
            var root = TestAssetsManager.CreateTestInstance("TestLibraryWithDocs")
                .WithBuildArtifacts();
            
            var outputXmlPath = Path.Combine(root.Path, "bin", "Debug", DefaultLibraryFramework, "TestLibrary.xml");

            new FileInfo(outputXmlPath)
                .Should().Exist("because xmlDoc=true is specified in project.json")
                   .And.ContainText("Gets the message from the helper", "because that is the intellisense doc comment in the TestLibraryWithDocs");
        }

        [Fact]
        public void When_project_has_resx_then_Sattelite_assembly_produced()
        {
            var root = TestAssetsManager.CreateTestInstance("TestProjectWithCultureSpecificResource")
                .WithBuildArtifacts();

            var generatedSatelliteAssemblyPath = Path.Combine(
                    root.Path,
                    "bin",
                    "Debug",
                    DefaultFramework,
                    "fr",
                    "TestProjectWithCultureSpecificResource.resources.dll");

            new FileInfo(generatedSatelliteAssemblyPath)
                .Should().Exist("Because the project includes Strings.fr.resx");
        }

        [Fact]
        public void When_PJ_references_analyzers_Then_they_are_executed()
        {
            var root = TestAssetsManager.CreateTestInstance("TestLibraryWithAnalyzer")
                .WithLockFiles();

            var buildResult = new TestCommand("dotnet")
                .WithWorkingDirectory(root.Path)
                .ExecuteWithCapturedOutput($"build -f {DefaultLibraryFramework}");

            buildResult
                .Should().Pass();

            buildResult.StdErr
                .Should().Contain("CA1018", "because this is produced by the analyzer");
        }

        [Fact]
        // TODO: this test is really just testing space in path. It does not validate anything about
        // PreserveCompilationContext
        public void CompilingAppWithPreserveCompilationContextWithSpaceInThePathShouldSucceed()
        {
            var root = TestAssetsManager.CreateTestInstance("TestAppCompilationContext", "space directory")
                .WithLockFiles();

            new TestCommand("dotnet")
                .WithWorkingDirectory(root.Path)
                .ExecuteWithCapturedOutput($"build -f {DefaultLibraryFramework}")
                .Should().Pass();
        }

        [Fact]
        public void ContentFilesAreCopied()
        {
            var testInstance = TestAssetsManager.CreateTestInstance("TestAppWithContentPackage")
                .WithBuildArtifacts();

            var outputDir = Path.Combine(testInstance.Path, "bin", "Debug", DefaultLibraryFramework);

            // verify the output xml file
            new DirectoryInfo(outputDir).Sub("scripts").Should()
                .Exist()
                .And.HaveFile("run.cmd", "because it is in the nupkg's dnxcore50 scripts directory")
                .And.HaveFile("config.xml", "because it is in the nupkg's dnxcore50 config directory");

            // verify embedded resources
            result.StdOut.Should()
                .Contain("AppWithContentPackage.dnf.png")
                .And.Contain("AppWithContentPackage.ui.png");
                
            // verify 'all' language files not included
            result.StdOut.Should().NotContain("AppWithContentPackage.dnf_all.png");
            result.StdOut.Should().NotContain("AppWithContentPackage.ui_all.png");
            
            // verify classes
            result.StdOut.Should().Contain("AppWithContentPackage.Foo");
            result.StdOut.Should().Contain("MyNamespace.Util");
        }

        [Fact]
        public void EmbeddedResourcesAreCopied()
        {
            var testInstance = TestAssetsManager.CreateTestInstance("EndToEndTestApp")
                                                .WithLockFiles()
                                                .WithBuildArtifacts();

            var root = testInstance.TestRoot;

            // run compile
            var outputDir = Path.Combine(root, "bin");
            var testProject = ProjectUtils.GetProjectJson(root, "EndToEndTestApp");
            var buildCommand = new BuildCommand(testProject, output: outputDir, framework: DefaultFramework);
            var result = buildCommand.ExecuteWithCapturedOutput();
            result.Should().Pass();

            var objDirInfo = new DirectoryInfo(Path.Combine(root, "obj", "Debug", DefaultFramework));
            objDirInfo.Should().HaveFile("EndToEndTestApp.resource1.resources");
            objDirInfo.Should().HaveFile("myresource.resources");
            objDirInfo.Should().HaveFile("EndToEndTestApp.defaultresource.resources");
        }

        [Fact]
        public void CopyToOutputFilesAreCopied()
        {
            var testInstance = TestAssetsManager.CreateTestInstance("EndToEndTestApp")
                                                .WithLockFiles()
                                                .WithBuildArtifacts();

            var root = testInstance.TestRoot;

            // run compile
            var outputDir = Path.Combine(root, "bin");
            var testProject = ProjectUtils.GetProjectJson(root, "EndToEndTestApp");
            var buildCommand = new BuildCommand(testProject, output: outputDir, framework: DefaultFramework);
            var result = buildCommand.ExecuteWithCapturedOutput();
            result.Should().Pass();

            var outputDirInfo = new DirectoryInfo(Path.Combine(outputDir, "copy"));
            outputDirInfo.Should().HaveFile("file.txt");
            outputDirInfo.Should().NotHaveFile("fileex.txt");
        }

        [Fact]
        public void CanSetOutputAssemblyNameForLibraries()
        {
            var testInstance =
                TestAssetsManager
                    .CreateTestInstance("LibraryWithOutputAssemblyName")
                    .WithLockFiles();

            var root = testInstance.TestRoot;
            var outputDir = Path.Combine(root, "bin");
            var testProject = ProjectUtils.GetProjectJson(root, "LibraryWithOutputAssemblyName");
            var buildCommand = new BuildCommand(testProject, output: outputDir, framework: DefaultLibraryFramework);
            var result = buildCommand.ExecuteWithCapturedOutput();
            result.Should().Pass();

            new DirectoryInfo(outputDir).Should().HaveFiles(new [] { "MyLibrary.dll" });
        }

        [Fact]
        public void CanSetOutputAssemblyNameForApps()
        {
            var testInstance =
                TestAssetsManager
                    .CreateTestInstance("AppWithOutputAssemblyName")
                    .WithLockFiles();

            var root = testInstance.TestRoot;
            var outputDir = Path.Combine(root, "bin");
            var testProject = ProjectUtils.GetProjectJson(root, "AppWithOutputAssemblyName");
            var buildCommand = new BuildCommand(testProject, output: outputDir, framework: DefaultFramework);
            var result = buildCommand.ExecuteWithCapturedOutput();
            result.Should().Pass();

            new DirectoryInfo(outputDir).Should().HaveFiles(
                new [] { "MyApp.dll", "MyApp" + buildCommand.GetExecutableExtension(),
                    "MyApp.runtimeconfig.json", "MyApp.deps.json" });
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
