// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.ProjectModel.Compilation;
using Microsoft.DotNet.ProjectModel.Graph;
using Microsoft.DotNet.ProjectModel.Resolution;
using Microsoft.DotNet.Tools.Test.Utilities;
using FluentAssertions;
using Xunit;

namespace Microsoft.DotNet.ProjectModel.Tests
{
    public class LibraryExporterPackageTests
    {
        private const string PackagePath = "PackagePath";

        private PackageDescription CreateDescription(LockFileTargetLibrary target = null, LockFilePackageLibrary package = null)
        {
            return new PackageDescription(PackagePath,
                package ?? new LockFilePackageLibrary(),
                target ?? new LockFileTargetLibrary(),
                new List<LibraryRange>(), compatible: true, resolved: true);
        }

        [Fact]
        public void ExportsPackageNativeLibraries()
        {
            var description = CreateDescription(
                new LockFileTargetLibrary()
                {
                    NativeLibraries = new List<LockFileItem>()
                    {
                        { new LockFileItem() { Path = "lib/Native.so" } }
                    }
                });

            var result = ExportSingle(description);
            result.NativeLibraryGroups.Should().HaveCount(1);

            var libraryAsset = result.NativeLibraryGroups.GetDefaultAssets().First();
            libraryAsset.Name.Should().Be("Native");
            libraryAsset.Transform.Should().BeNull();
            libraryAsset.RelativePath.Should().Be("lib/Native.so");
            libraryAsset.ResolvedPath.Should().Be(Path.Combine(PackagePath, "lib/Native.so"));
        }

        [Fact]
        public void ExportsPackageCompilationAssebmlies()
        {
            var description = CreateDescription(
                new LockFileTargetLibrary()
                {
                    CompileTimeAssemblies = new List<LockFileItem>()
                    {
                        { new LockFileItem() { Path = "ref/Native.dll" } }
                    }
                });

            var result = ExportSingle(description);
            result.CompilationAssemblies.Should().HaveCount(1);

            var libraryAsset = result.CompilationAssemblies.First();
            libraryAsset.Name.Should().Be("Native");
            libraryAsset.Transform.Should().BeNull();
            libraryAsset.RelativePath.Should().Be("ref/Native.dll");
            libraryAsset.ResolvedPath.Should().Be(Path.Combine(PackagePath, "ref/Native.dll"));
        }

        [Fact]
        public void ExportsPackageRuntimeAssebmlies()
        {
            var description = CreateDescription(
                new LockFileTargetLibrary()
                {
                    RuntimeAssemblies = new List<LockFileItem>()
                    {
                        { new LockFileItem() { Path = "ref/Native.dll" } }
                    }
                });

            var result = ExportSingle(description);
            result.RuntimeAssemblyGroups.Should().HaveCount(1);

            var libraryAsset = result.RuntimeAssemblyGroups.GetDefaultAssets().First();
            libraryAsset.Name.Should().Be("Native");
            libraryAsset.Transform.Should().BeNull();
            libraryAsset.RelativePath.Should().Be("ref/Native.dll");
            libraryAsset.ResolvedPath.Should().Be(Path.Combine(PackagePath, "ref/Native.dll"));
        }

        [Fact]
        public void ExportsPackageRuntimeTargets()
        {
            var description = CreateDescription(
                new LockFileTargetLibrary()
                {
                    RuntimeTargets = new List<LockFileRuntimeTarget>()
                    {
                        new LockFileRuntimeTarget("native/native.dylib", "osx", "native"),
                        new LockFileRuntimeTarget("lib/Something.OSX.dll", "osx", "runtime")
                    }
                });

            var result = ExportSingle(description);
            result.RuntimeAssemblyGroups.Should().HaveCount(2);
            result.RuntimeAssemblyGroups.First(g => g.Runtime == string.Empty).Assets.Should().HaveCount(0);
            result.RuntimeAssemblyGroups.First(g => g.Runtime == "osx").Assets.Should().HaveCount(1);
            result.NativeLibraryGroups.Should().HaveCount(2);
            result.NativeLibraryGroups.First(g => g.Runtime == string.Empty).Assets.Should().HaveCount(0);
            result.NativeLibraryGroups.First(g => g.Runtime == "osx").Assets.Should().HaveCount(1);

            var nativeAsset = result.NativeLibraryGroups.GetRuntimeAssets("osx").First();
            nativeAsset.Name.Should().Be("native");
            nativeAsset.Transform.Should().BeNull();
            nativeAsset.RelativePath.Should().Be("native/native.dylib");
            nativeAsset.ResolvedPath.Should().Be(Path.Combine(PackagePath, "native/native.dylib"));

            var runtimeAsset = result.RuntimeAssemblyGroups.GetRuntimeAssets("osx").First();
            runtimeAsset.Name.Should().Be("Something.OSX");
            runtimeAsset.Transform.Should().BeNull();
            runtimeAsset.RelativePath.Should().Be("lib/Something.OSX.dll");
            runtimeAsset.ResolvedPath.Should().Be(Path.Combine(PackagePath, "lib/Something.OSX.dll"));
        }

        [Fact]
        public void ExportsSources()
        {
            var description = CreateDescription(
               package: new LockFilePackageLibrary()
               {
                   Files = new List<string>()
                   {
                      Path.Combine("shared", "file.cs")
                   }
               });

            var result = ExportSingle(description);
            result.SourceReferences.Should().HaveCount(1);

            var libraryAsset = result.SourceReferences.First();
            libraryAsset.Name.Should().Be("file");
            libraryAsset.Transform.Should().BeNull();
            libraryAsset.RelativePath.Should().Be(Path.Combine("shared", "file.cs"));
            libraryAsset.ResolvedPath.Should().Be(Path.Combine(PackagePath, "shared", "file.cs"));
        }

        [Fact]
        public void ExportsCopyToOutputContentFiles()
        {
            var description = CreateDescription(
                new LockFileTargetLibrary()
                {
                    ContentFiles = new List<LockFileContentFile>()
                    {
                        new LockFileContentFile()
                        {
                            CopyToOutput = true,
                            Path = Path.Combine("content", "file.txt"),
                            OutputPath = Path.Combine("Out","Path.txt"),
                            PPOutputPath = "something"
                        }
                    }
                });

            var result = ExportSingle(description);
            result.RuntimeAssets.Should().HaveCount(1);

            var libraryAsset = result.RuntimeAssets.First();
            libraryAsset.Transform.Should().NotBeNull();
            libraryAsset.RelativePath.Should().Be(Path.Combine("Out", "Path.txt"));
            libraryAsset.ResolvedPath.Should().Be(Path.Combine(PackagePath, "content", "file.txt"));
        }


        [Fact]
        public void ExportsResourceContentFiles()
        {
            var description = CreateDescription(
                new LockFileTargetLibrary()
                {
                    ContentFiles = new List<LockFileContentFile>()
                    {
                        new LockFileContentFile()
                        {
                            BuildAction = BuildAction.EmbeddedResource,
                            Path = Path.Combine("content", "file.txt"),
                            PPOutputPath = "something"
                        }
                    }
                });

            var result = ExportSingle(description);
            result.EmbeddedResources.Should().HaveCount(1);

            var libraryAsset = result.EmbeddedResources.First();
            libraryAsset.Transform.Should().NotBeNull();
            libraryAsset.RelativePath.Should().Be(Path.Combine("content", "file.txt"));
            libraryAsset.ResolvedPath.Should().Be(Path.Combine(PackagePath, "content", "file.txt"));
        }

        [Fact]
        public void ExportsCompileContentFiles()
        {
            var description = CreateDescription(
                new LockFileTargetLibrary()
                {
                    ContentFiles = new List<LockFileContentFile>()
                    {
                        new LockFileContentFile()
                        {
                            BuildAction = BuildAction.Compile,
                            Path = Path.Combine("content", "file.cs"),
                            PPOutputPath = "something"
                        }
                    }
                });

            var result = ExportSingle(description);
            result.SourceReferences.Should().HaveCount(1);

            var libraryAsset = result.SourceReferences.First();
            libraryAsset.Transform.Should().NotBeNull();
            libraryAsset.RelativePath.Should().Be(Path.Combine("content", "file.cs"));
            libraryAsset.ResolvedPath.Should().Be(Path.Combine(PackagePath, "content", "file.cs"));
        }



        [Fact]
        public void SelectsContentFilesOfProjectCodeLanguage()
        {
            var description = CreateDescription(
                new LockFileTargetLibrary()
                {
                    ContentFiles = new List<LockFileContentFile>()
                    {
                            new LockFileContentFile()
                            {
                                BuildAction = BuildAction.Compile,
                                Path = Path.Combine("content", "file.cs"),
                                PPOutputPath = "something",
                                CodeLanguage = "cs"
                            },
                            new LockFileContentFile()
                            {
                                BuildAction = BuildAction.Compile,
                                Path = Path.Combine("content", "file.vb"),
                                PPOutputPath = "something",
                                CodeLanguage = "vb"
                            },
                            new LockFileContentFile()
                            {
                                BuildAction = BuildAction.Compile,
                                Path = Path.Combine("content", "file.any"),
                                PPOutputPath = "something",
                            }
                    }
                });

            var result = ExportSingle(description);
            result.SourceReferences.Should().HaveCount(1);

            var libraryAsset = result.SourceReferences.First();
            libraryAsset.Transform.Should().NotBeNull();
            libraryAsset.RelativePath.Should().Be(Path.Combine("content", "file.cs"));
            libraryAsset.ResolvedPath.Should().Be(Path.Combine(PackagePath, "content", "file.cs"));
        }

        [Fact]
        public void SelectsContentFilesWithNoLanguageIfProjectLanguageNotMathed()
        {
            var description = CreateDescription(
                new LockFileTargetLibrary()
                {
                    ContentFiles = new List<LockFileContentFile>()
                    {
                            new LockFileContentFile()
                            {
                                BuildAction = BuildAction.Compile,
                                Path = Path.Combine("content", "file.vb"),
                                PPOutputPath = "something",
                                CodeLanguage = "vb"
                            },
                            new LockFileContentFile()
                            {
                                BuildAction = BuildAction.Compile,
                                Path = Path.Combine("content", "file.any"),
                                PPOutputPath = "something",
                            }
                    }
                });

            var result = ExportSingle(description);
            result.SourceReferences.Should().HaveCount(1);

            var libraryAsset = result.SourceReferences.First();
            libraryAsset.Transform.Should().NotBeNull();
            libraryAsset.RelativePath.Should().Be(Path.Combine("content", "file.any"));
            libraryAsset.ResolvedPath.Should().Be(Path.Combine(PackagePath, "content", "file.any"));
        }

        private LibraryExport ExportSingle(LibraryDescription description = null)
        {
            var rootProject = new Project()
            {
                Name = "RootProject",
                CompilerName = "csc"
            };

            var rootProjectDescription = new ProjectDescription(
                new LibraryRange(),
                rootProject,
                new LibraryRange[] { },
                new TargetFrameworkInformation(),
                true);

            if (description == null)
            {
                description = rootProjectDescription;
            }
            else
            {
                description.Parents.Add(rootProjectDescription);
            }

            var libraryManager = new LibraryManager(new[] { description }, new DiagnosticMessage[] { }, "");
            var allExports = new LibraryExporter(rootProjectDescription, libraryManager, "config", "runtime", "basepath", "solutionroot").GetAllExports();
            var export = allExports.Single();
            return export;
        }

    }
}
