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
        [Fact]
        public void When_xmlDoc_is_true_then_doc_xml_is_generated()
        {
            var root = TestAssetsManager.CreateTestInstance("TestLibraryWithXmlDoc")
                .WithBuildArtifacts()
                .Path;
            
            var outputXmlPath = Path.Combine(root, "bin", "Debug", DefaultLibraryFramework, "TestLibraryWithXmlDoc.xml");

            new FileInfo(outputXmlPath)
                .Should().Exist("because xmlDoc=true is specified in project.json")
                .And.ContainText("Gets the message from the helper", "because that is the intellisense doc comment in the TestLibraryWithDocs");
        }

        [Fact]
        public void When_project_has_resx_then_Sattelite_assembly_produced()
        {
            var root = TestAssetsManager.CreateTestInstance("TestProjectWithCultureSpecificResource")
                .WithBuildArtifacts()
                .Path;

            var generatedSatelliteAssemblyPath = Path.Combine(
                    root,
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
                .WithLockFiles()
                .Path;

            var buildResult = new TestCommand("dotnet")
                .WithWorkingDirectory(root)
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
                .WithLockFiles()
                .Path;

            new TestCommand("dotnet")
                .WithWorkingDirectory(Path.Combine(root, "TestApp"))
                .ExecuteWithCapturedOutput($"build -f {DefaultFramework}")
                .Should().Pass();
        }

        [Fact]
        public void When_PJ_references_package_with_content_Then_it_is_included_in_the_project()
        {
            var root = TestAssetsManager.CreateTestInstance("TestAppWithContentPackage")
                .WithLockFiles()
                .Path;

            var result = new TestCommand("dotnet")
                .WithWorkingDirectory(root)
                .ExecuteWithCapturedOutput($"build -f {DefaultFramework}");

            var outputDir = Path.Combine(root, "bin", "Debug", DefaultFramework);

            result.Should().Pass();

            new DirectoryInfo(outputDir)
                .Should().HaveFile("config.xml", "because it is in the contentFiles/cs/dnxcore50/config directory");
                
            new DirectoryInfo(outputDir).Sub("scripts").Should()
                .Exist()
                .And.HaveFile("run.cmd", "because it is in the contentFiles/cs/dnxcore50/scripts directory");

            // verify embedded resources
            result.StdOut.Should()
                .Contain("AppWithContentPackage.dnf.png", "because it is in the contentFiles/cs/dnxcore50/images directory.")
                .And.Contain("AppWithContentPackage.ui.png", "because it is in the contentFiles/cs/dnxcore50/images directory.")
                .And.Contain("AppWithContentPackage.Foo", "because it is in the contentFiles/cs/dnxcore50/code directory.")
                .And.Contain("MyNamespace.Util", "because it is in the contentFiles/cs/dnxcore50/code directory.")
                .And.NotContain("AppWithContentPackage.dnf_all.png", "because it is in the contentFiles/any directory.")
                .And.NotContain("AppWithContentPackage.ui_all.png", "because it is in the contentFiles/any directory.");
            
        }

        [Fact]
        public void When_resources_are_embedded_Then_they_are_in_output()
        {
            var root = TestAssetsManager.CreateTestInstance("EndToEndTestApp")
                                                .WithLockFiles()
                                                .WithBuildArtifacts()
                                                .Path;

            new DirectoryInfo(Path.Combine(root, "obj", "Debug", DefaultFramework))
                .Should().HaveFile("EndToEndTestApp.resource1.resources", "because *.resx is embedded")
                .And.HaveFile("myresource.resources", "because resource2 got mapped to a new name");
            objDirInfo.Should().HaveFile("EndToEndTestApp.defaultresource.resources");
        }

        [Fact]
        public void When_copyToOutput_is_configured_Then_included_files_are_copied_except_excluded()
        {
            var root = TestAssetsManager.CreateTestInstance("EndToEndTestApp")
                                                .WithLockFiles()
                                                .WithBuildArtifacts()
                                                .Path;

            new DirectoryInfo(Path.Combine(root, "bin", "Debug", DefaultFramework, "copy"))
                .Should().HaveFile("file.txt", "because it is included by copyToOutput")
                .And.NotHaveFile("fileex.txt", "because it is excluded by copyToOutput");
        }

        [Fact]
        public void When_outputName_is_set_for_library_Then_the_library_bares_that_name()
        {
            var root = TestAssetsManager.CreateTestInstance("LibraryWithOutputAssemblyName")
                .WithLockFiles()
                .WithBuildArtifacts()
                .Path;

            new DirectoryInfo(Path.Combine(root, "bin", "Debug", DefaultLibraryFramework))
                .Should().HaveFile("MyLibrary.dll", "because that is the outputName")
                .And.NotHaveFile("LibraryWithOutputAssemblyName.dll", "because outputName overrides it");
        }

        [Fact]
        public void When_outputName_is_set_for_app_Then_the_app_bares_that_name()
        {
            var root = TestAssetsManager.CreateTestInstance("AppWithOutputAssemblyName")
                .WithLockFiles()
                .WithBuildArtifacts()
                .Path;
                
            new DirectoryInfo(Path.Combine(root, "bin", "Debug", DefaultFramework))
                .Should().HaveFiles(
                    new [] 
                    { 
                        "MyApp.dll", 
                        "MyApp.runtimeconfig.json", 
                        "MyApp.deps.json" 
                    }, "because that is the outputName")
                .And.NotHaveFiles(
                    new [] 
                    { 
                        "AppWithOutputAssemblyName.dll", 
                        "AppWithOutputAssemblyName.runtimeconfig.json", 
                        "AppWithOutputAssemblyName.deps.json" 
                    }, "because outputName is set");
        }
    }
}
