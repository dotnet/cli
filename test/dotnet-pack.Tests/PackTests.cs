// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using FluentAssertions;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;

namespace Microsoft.DotNet.Tools.Compiler.Tests
{
    public class PackTests : TestBase
    {
        private readonly string _testProjectsRoot;

        [Fact]
        public void OutputsPackagesToConfigurationSubdirWhenOutputParameterIsNotPassed()
        {
            var testInstance = TestAssets.Get("TestLibraryWithConfiguration")
                                        .CreateInstance()
                                        .WithSourceFiles()
                                        .WithRestoreFiles()
                                        .WithBuildFiles();

            var packCommand = new PackCommand(configuration: "Test")
                .WithWorkingDirectory(testInstance.Root);

            var result = packCommand.Execute();

            result.Should().Pass();

            var outputDir = new DirectoryInfo(Path.Combine(testInstance.Root.FullName, "bin", "Test"));

            outputDir.Should().Exist()
                          .And.HaveFiles(new [] 
                                            { 
                                                "TestLibraryWithConfiguration.1.0.0.nupkg", 
                                                "TestLibraryWithConfiguration.1.0.0.symbols.nupkg" 
                                            });
        }

        [Fact]
        public void OutputsPackagesFlatIntoOutputDirWhenOutputParameterIsPassed()
        {
            var testInstance = TestAssets.Get("TestLibraryWithConfiguration")
                .CreateInstance()
                .WithSourceFiles()
                .WithBuildFiles()
                .WithRestoreFiles();

            var outputDir = new DirectoryInfo(Path.Combine(testInstance.Root.FullName, "bin2"));

            var packCommand = new PackCommand(output: outputDir.FullName)
                .WithWorkingDirectory(testInstance.Root)
                .Execute()
                .Should().Pass();

            outputDir.Should().Exist()
                          .And.HaveFiles(new [] 
                                            { 
                                                "TestLibraryWithConfiguration.1.0.0.nupkg", 
                                                "TestLibraryWithConfiguration.1.0.0.symbols.nupkg" 
                                            });
        }

        [Fact]
        public void SettingVersionSuffixFlag_ShouldStampAssemblyInfoInOutputAssemblyAndPackage()
        {
            var testInstance = TestAssets.Get("TestLibraryWithConfiguration")
                .CreateInstance()
                .WithSourceFiles()
                .WithRestoreFiles();

            var cmd = new PackCommand(Path.Combine(testInstance.Root.FullName, Project.FileName),  versionSuffix: "85");
            
            cmd.Execute().Should().Pass();

            var output = Path.Combine(testInstance.Root.FullName, "bin", "Debug", DefaultLibraryFramework, "TestLibraryWithConfiguration.dll");
            
            var informationalVersion = PeReaderUtils.GetAssemblyAttributeValue(output, "AssemblyInformationalVersionAttribute");

            informationalVersion.Should().NotBeNull()
                                .And.BeEquivalentTo("1.0.0-85");

            var outputPackage = Path.Combine(testInstance.Root.FullName, "bin", "Debug", "TestLibraryWithConfiguration.1.0.0-85.nupkg");
            
            new FileInfo(outputPackage).Should().Exist();
        }

        [Fact]
        public void HasBuildOutputWhenUsingBuildBasePath()
        {
            var testInstance = TestAssets.Get("TestLibraryWithConfiguration")
                                         .CreateInstance()
                                         .WithSourceFiles()
                                         .WithRestoreFiles();

            new PackCommand(buildBasePath: "buildBase")
                .WithWorkingDirectory(testInstance.Root)
                .Execute().Should().Pass();

            var outputPackage = new FileInfo(Path.Combine(testInstance.Root.FullName, "bin", "Debug", "TestLibraryWithConfiguration.1.0.0.nupkg"));
                
            outputPackage.Should().Exist();

            ZipFile.Open(outputPackage.FullName, ZipArchiveMode.Read)
                   .Entries
                   .Should().Contain(e => e.FullName == "lib/netstandard1.5/TestLibraryWithConfiguration.dll");
        }

        [Fact]
        public void HasIncludedFiles()
        {
            var testInstance = TestAssets.Get("EndToEndTestApp")
                .CreateInstance()
                .WithSourceFiles()
                .WithRestoreFiles()
                .WithBuildFiles();

            var cmd = new PackCommand()
                .WithWorkingDirectory(testInstance.Root)
                .Execute()
                .Should().Pass();

            var outputPackage = new FileInfo(Path.Combine(testInstance.Root.FullName, "bin", "Debug", "EndToEndTestApp.1.0.0.nupkg"));
            
            outputPackage.Should().Exist();

            ZipFile.Open(outputPackage.FullName, ZipArchiveMode.Read)
                .Entries
                .Should().Contain(e => e.FullName == "packfiles/pack1.txt")
                     .And.Contain(e => e.FullName == "newpath/pack2.txt")
                     .And.Contain(e => e.FullName == "anotherpath/pack2.txt");
        }

        [Fact]
        public void PackAddsCorrectFilesForProjectsWithOutputNameSpecified()
        {
            var testInstance = TestAssets.Get("LibraryWithOutputAssemblyName")
                    .CreateInstance()
                    .WithSourceFiles()
                    .WithRestoreFiles();

            new PackCommand()
                .WithWorkingDirectory(testInstance.Root)
                .Execute()
                .Should().Pass();

            var outputPackage = new FileInfo(Path.Combine(testInstance.Root.FullName, "bin", "Debug", "LibraryWithOutputAssemblyName.1.0.0.nupkg"));
            
            outputPackage.Should().Exist();

            ZipFile.Open(outputPackage.FullName, ZipArchiveMode.Read)
                .Entries
                .Should().Contain(e => e.FullName == "lib/netstandard1.5/MyLibrary.dll");

            var symbolsPackage = new FileInfo(Path.Combine(testInstance.Root.FullName, "bin", "Debug", "LibraryWithOutputAssemblyName.1.0.0.symbols.nupkg"));
            
            symbolsPackage.Should().Exist();

            ZipFile.Open(symbolsPackage.FullName, ZipArchiveMode.Read)
                .Entries
                .Should().Contain(e => e.FullName == "lib/netstandard1.5/MyLibrary.dll")
                     .And.Contain(e => e.FullName == "lib/netstandard1.5/MyLibrary.pdb");
        }

        [Fact]
        public void PackWorksWithLocalProjectJson()
        {
            var testInstance = TestAssets.Get("TestAppSimple")
                .CreateInstance()
                .WithSourceFiles()
                .WithRestoreFiles();

            new PackCommand()
                .WithWorkingDirectory(testInstance.Root.FullName)
                .Execute()
                .Should().Pass();
        }

        [Fact]
        public void HasServiceableFlagWhenArgumentPassed()
        {
            var testInstance = TestAssets.Get("TestLibraryWithConfiguration")
                .CreateInstance()
                .WithSourceFiles()
                .WithRestoreFiles()
                .WithBuildFiles();

            var packCommand = new PackCommand(configuration: "Debug", serviceable: true)
                                    .WithWorkingDirectory(testInstance.Root);

            var result = packCommand.Execute();

            result.Should().Pass();

            var outputDir = new DirectoryInfo(Path.Combine(testInstance.Root.FullName, "bin", "Debug"));

            outputDir.Should().Exist()
                          .And.HaveFiles(new []  
                                            { 
                                                "TestLibraryWithConfiguration.1.0.0.nupkg", 
                                                "TestLibraryWithConfiguration.1.0.0.symbols.nupkg" 
                                            });

            var outputPackage = outputDir.GetFile("TestLibraryWithConfiguration.1.0.0.nupkg");

            var zip = ZipFile.Open(outputPackage, ZipArchiveMode.Read);

            zip.Entries.Should().Contain(e => e.FullName == "TestLibraryWithConfiguration.nuspec");

            var manifestReader = new StreamReader(zip.Entries.First(e => e.FullName == "TestLibraryWithConfiguration.nuspec").Open());

            var nuspecXml = XDocument.Parse(manifestReader.ReadToEnd());

            var node = nuspecXml.Descendants().Single(e => e.Name.LocalName == "serviceable");

            Assert.Equal("true", node.Value);
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
