﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Build.Construction;
using Microsoft.DotNet.TestFramework;
using Microsoft.DotNet.Tools.Test.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using FluentAssertions;
using System.IO;
using Microsoft.DotNet.Tools.Migrate;
using BuildCommand = Microsoft.DotNet.Tools.Test.Utilities.BuildCommand;
using System.Runtime.Loader;
using Newtonsoft.Json.Linq;

using MigrateCommand = Microsoft.DotNet.Tools.Migrate.MigrateCommand;

namespace Microsoft.DotNet.Migration.Tests
{
    public class GivenThatIWantToMigrateTestApps : TestBase
    {
        [Theory]
        [InlineData("TestAppWithRuntimeOptions")]
        [InlineData("TestAppWithContents")]
        [InlineData("AppWithAssemblyInfo")]
        [InlineData("TestAppWithEmbeddedResources")]
        public void ItMigratesApps(string projectName)
        {
            var projectDirectory = TestAssetsManager.CreateTestInstance(projectName, identifier: projectName)
                                                    .WithLockFiles()
                                                    .Path;

            CleanBinObj(projectDirectory);

            var outputComparisonData = BuildProjectJsonMigrateBuildMSBuild(projectDirectory, projectName);

            var outputsIdentical =
                outputComparisonData.ProjectJsonBuildOutputs.SetEquals(outputComparisonData.MSBuildBuildOutputs);

            if (!outputsIdentical)
            {
                OutputDiagnostics(outputComparisonData);
            }

            outputsIdentical.Should().BeTrue();

            VerifyAllMSBuildOutputsRunnable(projectDirectory);

            var outputCsProj = Path.Combine(projectDirectory, projectName + ".csproj");
            var csproj = File.ReadAllText(outputCsProj);
            csproj.EndsWith("\n").Should().Be(true);
        }

        [Fact]
        public void ItMigratesSignedApps()
        {
            var projectDirectory = TestAssetsManager.CreateTestInstance("TestAppWithSigning").WithLockFiles().Path;

            CleanBinObj(projectDirectory);

            var outputComparisonData = BuildProjectJsonMigrateBuildMSBuild(projectDirectory, "TestAppWithSigning");

            var outputsIdentical =
                outputComparisonData.ProjectJsonBuildOutputs.SetEquals(outputComparisonData.MSBuildBuildOutputs);

            if (!outputsIdentical)
            {
                OutputDiagnostics(outputComparisonData);
            }

            outputsIdentical.Should().BeTrue();

            VerifyAllMSBuildOutputsRunnable(projectDirectory);

            VerifyAllMSBuildOutputsAreSigned(projectDirectory);
        }

        [Fact]
        public void ItMigratesDotnetNewConsoleWithIdenticalOutputs()
        {
            var testInstance = TestAssetsManager
                .CreateTestInstance("ProjectJsonConsoleTemplate");
            
            var projectDirectory = testInstance.Path;

            var outputComparisonData = GetComparisonData(projectDirectory);

            var outputsIdentical =
                outputComparisonData.ProjectJsonBuildOutputs.SetEquals(outputComparisonData.MSBuildBuildOutputs);

            if (!outputsIdentical)
            {
                OutputDiagnostics(outputComparisonData);
            }

            outputsIdentical.Should().BeTrue();

            VerifyAllMSBuildOutputsRunnable(projectDirectory);
        }

        [Fact]
        public void ItMigratesOldDotnetNewWebWithoutToolsWithOutputsContainingProjectJsonOutputs()
        {
            var testInstance = TestAssetsManager
                .CreateTestInstance("ProjectJsonWebTemplate")
                .WithLockFiles();

            var projectDirectory = testInstance.Path;

            var globalDirectory = Path.Combine(projectDirectory, "..");  
            var projectJsonFile = Path.Combine(projectDirectory, "project.json");  
              
            WriteGlobalJson(globalDirectory);

            var outputComparisonData = GetComparisonData(projectDirectory);

            var outputsIdentical =
                outputComparisonData.ProjectJsonBuildOutputs.SetEquals(outputComparisonData.MSBuildBuildOutputs);

            if (!outputsIdentical)
            {
                OutputDiagnostics(outputComparisonData);
            }

            outputsIdentical.Should().BeTrue();
        }

        [Fact]
        public void ItMigratesAndPublishesWebApp()
        {
            const string projectName = "WebAppWithMissingFileInPublishOptions";
            var testInstance = TestAssets.Get(projectName)
                .CreateInstance()
                .WithSourceFiles();

            var projectDirectory = testInstance.Root.FullName;

            MigrateProject(new [] { projectDirectory });

            Restore(projectDirectory);
            PublishMSBuild(projectDirectory, projectName);
        }

        [Fact]
        public void ItAddsMicrosoftNetWebSdkToTheSdkAttributeOfAWebApp()
        {
            var testInstance = TestAssetsManager
                .CreateTestInstance("ProjectJsonWebTemplate")
                .WithLockFiles();

            var projectDirectory = testInstance.Path;

            var globalDirectory = Path.Combine(projectDirectory, "..");  
            var projectJsonFile = Path.Combine(projectDirectory, "project.json");  
              
            MigrateProject(new [] { projectDirectory });

            var csProj = Path.Combine(projectDirectory, $"{new DirectoryInfo(projectDirectory).Name}.csproj");

            File.ReadAllText(csProj).Should().Contain(@"Sdk=""Microsoft.NET.Sdk.Web""");
        }

        [Theory]
        [InlineData("TestLibraryWithTwoFrameworks")]
        public void ItMigratesProjectsWithMultipleTFMs(string projectName)
        {
            var projectDirectory =
                TestAssetsManager.CreateTestInstance(projectName, identifier: projectName).WithLockFiles().Path;

            var outputComparisonData = BuildProjectJsonMigrateBuildMSBuild(projectDirectory, projectName);

            var outputsIdentical =
                outputComparisonData.ProjectJsonBuildOutputs.SetEquals(outputComparisonData.MSBuildBuildOutputs);

            if (!outputsIdentical)
            {
                OutputDiagnostics(outputComparisonData);
            }

            outputsIdentical.Should().BeTrue();
        }

        [Theory]
        [InlineData("TestAppWithLibrary/TestLibrary")]
        [InlineData("TestLibraryWithAnalyzer")]
        [InlineData("PJTestLibraryWithConfiguration")]
        public void ItMigratesALibrary(string projectName)
        {
            var projectDirectory =
                TestAssetsManager.CreateTestInstance(projectName, identifier: projectName).WithLockFiles().Path;

            var outputComparisonData = BuildProjectJsonMigrateBuildMSBuild(projectDirectory, Path.GetFileNameWithoutExtension(projectName));

            var outputsIdentical =
                outputComparisonData.ProjectJsonBuildOutputs.SetEquals(outputComparisonData.MSBuildBuildOutputs);

            if (!outputsIdentical)
            {
                OutputDiagnostics(outputComparisonData);
            }

            outputsIdentical.Should().BeTrue();
        }

        [Theory]
        [InlineData("ProjectA", "ProjectA,ProjectB,ProjectC,ProjectD,ProjectE")]
        [InlineData("ProjectB", "ProjectB,ProjectC,ProjectD,ProjectE")]
        [InlineData("ProjectC", "ProjectC,ProjectD,ProjectE")]
        [InlineData("ProjectD", "ProjectD")]
        [InlineData("ProjectE", "ProjectE")]
        public void ItMigratesRootProjectAndReferences(string projectName, string expectedProjects)
        {
            var projectDirectory =
                TestAssetsManager.CreateTestInstance("TestAppDependencyGraph", callingMethod: $"{projectName}.RefsTest").Path;

            MigrateProject(new [] { Path.Combine(projectDirectory, projectName) });

            string[] migratedProjects = expectedProjects.Split(new char[] { ',' });

            VerifyMigration(migratedProjects, projectDirectory);
         }

        [Theory]
        [InlineData("ProjectA")]
        [InlineData("ProjectB")]
        [InlineData("ProjectC")]
        [InlineData("ProjectD")]
        [InlineData("ProjectE")]
        public void ItMigratesRootProjectAndSkipsReferences(string projectName)
        {
            var projectDirectory =
                TestAssetsManager.CreateTestInstance("TestAppDependencyGraph", callingMethod: $"{projectName}.SkipRefsTest").Path;

            MigrateProject(new [] { Path.Combine(projectDirectory, projectName), "--skip-project-references" });

            VerifyMigration(Enumerable.Repeat(projectName, 1), projectDirectory);
         }

         [Theory]
         [InlineData(true)]
         [InlineData(false)]
         public void ItMigratesAllProjectsInGivenDirectory(bool skipRefs)
         {
            var projectDirectory = TestAssetsManager.CreateTestInstance("TestAppDependencyGraph", callingMethod: $"MigrateDirectory.SkipRefs.{skipRefs}").Path;

            if (skipRefs)
            {
                MigrateProject(new [] { projectDirectory, "--skip-project-references" });
            }
            else
            {
                MigrateProject(new [] { projectDirectory });
            }

            string[] migratedProjects = new string[] { "ProjectA", "ProjectB", "ProjectC", "ProjectD", "ProjectE", "ProjectF", "ProjectG", "ProjectH", "ProjectI", "ProjectJ" };

            VerifyMigration(migratedProjects, projectDirectory);
         }

         [Fact]
         public void ItMigratesGivenProjectJson()
         {
            var projectDirectory = TestAssetsManager.CreateTestInstance("TestAppDependencyGraph").Path;

            var project = Path.Combine(projectDirectory, "ProjectA", "project.json");

            MigrateProject(new [] { project });

            string[] migratedProjects = new string[] { "ProjectA", "ProjectB", "ProjectC", "ProjectD", "ProjectE" };

            VerifyMigration(migratedProjects, projectDirectory);
         }

         [Fact]
         // regression test for https://github.com/dotnet/cli/issues/4269
         public void ItMigratesAndBuildsP2PReferences()
         {
            var assetsDir = TestAssetsManager.CreateTestInstance("TestAppDependencyGraph").WithLockFiles().Path;

            var projectDirectory = Path.Combine(assetsDir, "ProjectF");

            var restoreDirectories = new string[]
            {
                projectDirectory, 
                Path.Combine(assetsDir, "ProjectG")
            };

            var outputComparisonData = BuildProjectJsonMigrateBuildMSBuild(projectDirectory, "ProjectF", new [] { projectDirectory }, restoreDirectories);

            var outputsIdentical = outputComparisonData.ProjectJsonBuildOutputs
                                                       .SetEquals(outputComparisonData.MSBuildBuildOutputs);

            if (!outputsIdentical)
            {
                OutputDiagnostics(outputComparisonData);
            }

            outputsIdentical.Should().BeTrue();

            VerifyAllMSBuildOutputsRunnable(projectDirectory);
         }

         [Theory]
         [InlineData("src", "ProjectH")]
         [InlineData("src with spaces", "ProjectJ")]
         public void ItMigratesAndBuildsProjectsInGlobalJson(string path, string projectName)
         {
            var assetsDir = TestAssetsManager.CreateTestInstance(Path.Combine("TestAppDependencyGraph", "ProjectsWithGlobalJson"), 
                                                                 callingMethod: $"ProjectsWithGlobalJson.{projectName}")
                                             .WithLockFiles().Path;
            var globalJson = Path.Combine(assetsDir, "global.json");

            var restoreDirectories = new string[]
            {
                Path.Combine(assetsDir, "src", "ProjectH"),
                Path.Combine(assetsDir, "src", "ProjectI"),
                Path.Combine(assetsDir, "src with spaces", "ProjectJ")
            };

            var projectDirectory = Path.Combine(assetsDir, path, projectName);

            var outputComparisonData = BuildProjectJsonMigrateBuildMSBuild(projectDirectory, 
                                                                           projectName,
                                                                           new [] { globalJson },
                                                                           restoreDirectories);

            var outputsIdentical = outputComparisonData.ProjectJsonBuildOutputs
                                                       .SetEquals(outputComparisonData.MSBuildBuildOutputs);

            if (!outputsIdentical)
            {
                OutputDiagnostics(outputComparisonData);
            }

            outputsIdentical.Should().BeTrue();
            
            VerifyAllMSBuildOutputsRunnable(projectDirectory);
         }
        
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void MigrationOutputsErrorWhenNoProjectsFound(bool useGlobalJson)
        {
            var projectDirectory = TestAssetsManager.CreateTestDirectory("Migration_outputs_error_when_no_projects_found");

            string argstr = string.Empty;

            string errorMessage = string.Empty;

            if (useGlobalJson)
            {
                var globalJsonPath = Path.Combine(projectDirectory.Path, "global.json");

                using (FileStream fs = File.Create(globalJsonPath))
                {
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        sw.WriteLine("{");
                        sw.WriteLine("\"projects\": [ \".\" ]");
                        sw.WriteLine("}");
                    }
                }

                argstr = globalJsonPath;

                errorMessage = "Unable to find any projects in global.json";
            }
            else
            {
                argstr = projectDirectory.Path;

                errorMessage = $"No project.json file found in '{projectDirectory.Path}'";
            }

            var result = new TestCommand("dotnet")
                .WithWorkingDirectory(projectDirectory.Path)
                .ExecuteWithCapturedOutput($"migrate {argstr}");

            // Expecting an error exit code.
            result.ExitCode.Should().Be(1);

            // Verify the error messages. Note that debug builds also show the call stack, so we search
            // for the error strings that should be present (rather than an exact match).
            result.StdErr.Should().Contain(errorMessage);
            result.StdErr.Should().Contain("Migration failed.");
        }

        [Fact]
        public void ItMigratesAndPublishesProjectsWithRuntimes()
        {
            var projectName = "PJTestAppSimple";
            var projectDirectory = TestAssetsManager
                .CreateTestInstance(projectName)
                .WithLockFiles()
                .Path;

            CleanBinObj(projectDirectory);
            BuildProjectJsonMigrateBuildMSBuild(projectDirectory, projectName);
            PublishMSBuild(projectDirectory, projectName, "win7-x64");
        }

        [WindowsOnlyTheory]
        [InlineData("DesktopTestProjects", "AutoAddDesktopReferencesDuringMigrate", true)]
        [InlineData("TestProjects", "PJTestAppSimple", false)]
        public void ItAutoAddDesktopReferencesDuringMigrate(string testGroup, string projectName, bool isDesktopApp)
        {
            var runtime = DotnetLegacyRuntimeIdentifiers.InferLegacyRestoreRuntimeIdentifier();
            var testAssetManager = GetTestGroupTestAssetsManager(testGroup);
            var projectDirectory = testAssetManager.CreateTestInstance(projectName).WithLockFiles().Path;
            
            CleanBinObj(projectDirectory);
            MigrateProject(new string[] { projectDirectory });
            Restore(projectDirectory, runtime: runtime);
            BuildMSBuild(projectDirectory, projectName, runtime:runtime);
            VerifyAutoInjectedDesktopReferences(projectDirectory, projectName, isDesktopApp);
            VerifyAllMSBuildOutputsRunnable(projectDirectory);
        }

        [Fact]
        public void ItBuildsAMigratedAppWithAnIndirectDependency()
        {
            const string projectName = "ProjectA";
            var solutionDirectory =
                TestAssetsManager.CreateTestInstance("TestAppDependencyGraph", callingMethod: "p").Path;
            var projectDirectory = Path.Combine(solutionDirectory, projectName);

            MigrateProject(new string[] { projectDirectory });
            Restore(projectDirectory);
            BuildMSBuild(projectDirectory, projectName);

            VerifyAllMSBuildOutputsRunnable(projectDirectory);
        }

        [Fact]
        public void ItMigratesProjectWithOutputName()
        {
            string projectName = "AppWithOutputAssemblyName";
            string expectedOutputName = "MyApp";

            var projectDirectory = TestAssetsManager.CreateTestInstance(projectName, callingMethod: $"It_migrates_{projectName}")
                .WithLockFiles()
                .Path;
            
            string expectedCsprojPath = Path.Combine(projectDirectory, $"{projectName}.csproj");
            if (File.Exists(expectedCsprojPath))
            {
                File.Delete(expectedCsprojPath);
            }

            CleanBinObj(projectDirectory);
            MigrateProject(projectDirectory);
            File.Exists(expectedCsprojPath).Should().BeTrue();
            Restore(projectDirectory, projectName);
            BuildMSBuild(projectDirectory, projectName);
            Directory.EnumerateFiles(
                    Path.Combine(projectDirectory, "bin"),
                    $"{expectedOutputName}.pdb",
                    SearchOption.AllDirectories)
                .Count().Should().Be(1);

            PackMSBuild(projectDirectory, projectName);
            Directory.EnumerateFiles(
                    Path.Combine(projectDirectory, "bin"),
                    $"{projectName}.1.0.0.nupkg",
                    SearchOption.AllDirectories)
                .Count().Should().Be(1);
        }

        [Theory]
        [InlineData("LibraryWithoutNetStandardLibRef")]
        [InlineData("LibraryWithNetStandardLibRef")]
        public void ItMigratesAndBuildsLibrary(string projectName)
        {
            var projectDirectory = TestAssetsManager.CreateTestInstance(projectName,
                callingMethod: $"{nameof(ItMigratesAndBuildsLibrary)}-projectName").Path;

            MigrateProject(projectDirectory);
            Restore(projectDirectory, projectName);
            BuildMSBuild(projectDirectory, projectName);
        }

        [Fact]
        public void ItFailsGracefullyWhenMigratingAppWithMissingDependency()
        {
            string projectName = "MigrateAppWithMissingDep";
            var projectDirectory = Path.Combine(GetTestGroupTestAssetsManager("NonRestoredTestProjects").CreateTestInstance(projectName).Path, "MyApp");

            string migrationOutputFile = Path.Combine(projectDirectory, "migration-output.json");
            File.Exists(migrationOutputFile).Should().BeFalse();
            MigrateCommand.Run(new string[] { projectDirectory, "-r", migrationOutputFile, "--format-report-file-json" }).Should().NotBe(0);
            File.Exists(migrationOutputFile).Should().BeTrue();
            File.ReadAllText(migrationOutputFile).Should().Contain("MIGRATE1018");
        }

        private void VerifyAutoInjectedDesktopReferences(string projectDirectory, string projectName, bool shouldBePresent)
        {
            if (projectName != null)
            {
                projectName = projectName + ".csproj";
            }

            var root = ProjectRootElement.Open(Path.Combine(projectDirectory, projectName));
            var autoInjectedReferences = root.Items.Where(i => i.ItemType == "Reference" && (i.Include == "System" || i.Include == "Microsoft.CSharp"));
            if (shouldBePresent)
            {
                autoInjectedReferences.Should().HaveCount(2);
            }
            else
            {
                autoInjectedReferences.Should().BeEmpty();
            }
        }

        private void VerifyMigration(IEnumerable<string> expectedProjects, string rootDir)
         {
             var migratedProjects = Directory.EnumerateFiles(rootDir, "*.csproj", SearchOption.AllDirectories)
                                             .Where(s => Directory.EnumerateFiles(Path.GetDirectoryName(s), "*.csproj").Count() == 1)
                                             .Where(s => Path.GetFileName(Path.GetDirectoryName(s)).Contains("Project"))
                                             .Select(s => Path.GetFileName(Path.GetDirectoryName(s)));

             migratedProjects.Should().BeEquivalentTo(expectedProjects);
         }

        private MigratedBuildComparisonData GetComparisonData(string projectDirectory)
        {
            File.Copy("NuGet.tempaspnetpatch.config", Path.Combine(projectDirectory, "NuGet.Config"));
            
            RestoreProjectJson(projectDirectory);

            var outputComparisonData =
                BuildProjectJsonMigrateBuildMSBuild(projectDirectory, Path.GetFileNameWithoutExtension(projectDirectory));

            return outputComparisonData;
        }

        private void VerifyAllMSBuildOutputsRunnable(string projectDirectory)
        {
            var dllFileName = Path.GetFileName(projectDirectory) + ".dll";

            var runnableDlls = Directory.EnumerateFiles(Path.Combine(projectDirectory, "bin"), dllFileName,
                SearchOption.AllDirectories);

            foreach (var dll in runnableDlls)
            {
                new TestCommand("dotnet").ExecuteWithCapturedOutput($"\"{dll}\"").Should().Pass();
            }
        }

        private void VerifyAllMSBuildOutputsAreSigned(string projectDirectory)
        {
            var dllFileName = Path.GetFileName(projectDirectory) + ".dll";

            var runnableDlls = Directory.EnumerateFiles(Path.Combine(projectDirectory, "bin"), dllFileName,
                SearchOption.AllDirectories);

            foreach (var dll in runnableDlls)
            {
                var assemblyName = AssemblyLoadContext.GetAssemblyName(dll);

                var token = assemblyName.GetPublicKeyToken();

                token.Should().NotBeNullOrEmpty();
            }
        }

        private MigratedBuildComparisonData BuildProjectJsonMigrateBuildMSBuild(string projectDirectory, 
                                                                                string projectName)
        {
            return BuildProjectJsonMigrateBuildMSBuild(projectDirectory, 
                                                       projectName,
                                                       new [] { projectDirectory }, 
                                                       new [] { projectDirectory });
        }

        private MigratedBuildComparisonData BuildProjectJsonMigrateBuildMSBuild(string projectDirectory, 
                                                                                string projectName,
                                                                                string[] migrateArgs,
                                                                                string[] restoreDirectories)
        {
            BuildProjectJson(projectDirectory);

            var projectJsonBuildOutputs = new HashSet<string>(CollectBuildOutputs(projectDirectory));

            CleanBinObj(projectDirectory);

            // Remove lock file for migration
            foreach(var dir in restoreDirectories)
            {
                File.Delete(Path.Combine(dir, "project.lock.json"));
            }

            MigrateProject(migrateArgs);

            DeleteXproj(projectDirectory);

            foreach(var dir in restoreDirectories)
            {
                Restore(dir);
            }

            BuildMSBuild(projectDirectory, projectName);

            var msbuildBuildOutputs = new HashSet<string>(CollectBuildOutputs(projectDirectory));

            return new MigratedBuildComparisonData(projectJsonBuildOutputs, msbuildBuildOutputs);
        }

        private IEnumerable<string> CollectBuildOutputs(string projectDirectory)
        {
            var fullBinPath = Path.GetFullPath(Path.Combine(projectDirectory, "bin"));

            return Directory.EnumerateFiles(fullBinPath, "*", SearchOption.AllDirectories)
                            .Select(p => Path.GetFullPath(p).Substring(fullBinPath.Length));
        }

        private static void DeleteDirectory(string dir)
        {
            foreach (string directory in Directory.EnumerateDirectories(dir))
            {
                DeleteDirectory(directory);
            }

            try
            {
                Directory.Delete(dir, true);
            }
            catch
            {
                // retry, if still doesn't delete then throw
                Directory.Delete(dir, true);
            }
        }

        private void CleanBinObj(string projectDirectory)
        {
            var dirs = new string[] { Path.Combine(projectDirectory, "bin"), Path.Combine(projectDirectory, "obj") };

            foreach (var dir in dirs)
            {
                if(Directory.Exists(dir))
                {
                    DeleteDirectory(dir);
                }
            }
        }

        private void BuildProjectJson(string projectDirectory)
        {
            Console.WriteLine(projectDirectory);
            var projectFile = "\"" + Path.Combine(projectDirectory, "project.json") + "\"";

            var result = new BuildPJCommand()
                .WithCapturedOutput()
                .Execute(projectFile);

            result.Should().Pass();
        }

        private void MigrateProject(params string[] migrateArgs)
        {
            var result =
                MigrateCommand.Run(migrateArgs);

            result.Should().Be(0);
        }

        private void RestoreProjectJson(string projectDirectory)
        {
            new TestCommand("dotnet")
                .WithWorkingDirectory(projectDirectory)
                .Execute("restore-projectjson")
                .Should().Pass();
        }

        private void Restore(string projectDirectory, string projectName=null, string runtime=null)
        {
            var command = new RestoreCommand()
                .WithWorkingDirectory(projectDirectory)
                .WithRuntime(runtime);

            if (projectName != null)
            {
                if (!Path.HasExtension(projectName))
                {
                    projectName += ".csproj";
                }
                command.Execute($"{projectName} /p:SkipInvalidConfigurations=true;_InvalidConfigurationWarning=false")
                    .Should().Pass();
            }
            else
            {
                command.Execute("/p:SkipInvalidConfigurations=true;_InvalidConfigurationWarning=false")
                    .Should().Pass(); 
            }
        }

        private string BuildMSBuild(
            string projectDirectory,
            string projectName,
            string configuration="Debug",
            string runtime=null)
        {
            if (projectName != null && !Path.HasExtension(projectName))
            {
                projectName = projectName + ".csproj";
            }

            DeleteXproj(projectDirectory);

            var result = new BuildCommand()
                .WithWorkingDirectory(projectDirectory)
                .WithRuntime(runtime)
                .ExecuteWithCapturedOutput($"{projectName} /p:Configuration={configuration}");

            result
                .Should().Pass();

            return result.StdOut;
        }

        private string PublishMSBuild(
            string projectDirectory,
            string projectName,
            string runtime = null,
            string configuration = "Debug")
        {
            if (projectName != null)
            {
                projectName = projectName + ".csproj";
            }

            DeleteXproj(projectDirectory);

            var result = new PublishCommand()
                .WithRuntime(runtime)
                .WithWorkingDirectory(projectDirectory)
                .ExecuteWithCapturedOutput($"{projectName} /p:Configuration={configuration}");

            result.Should().Pass();

            return result.StdOut;
        }

        private string PackMSBuild(string projectDirectory, string projectName)
        {
            if (projectName != null && !Path.HasExtension(projectName))
            {
                projectName = projectName + ".csproj";
            }

            var result = new PackCommand()
                .WithWorkingDirectory(projectDirectory)
                .ExecuteWithCapturedOutput($"{projectName}");

            result.Should().Pass();

            return result.StdOut;
        }

        private void DeleteXproj(string projectDirectory)
        {
            var xprojFiles = Directory.EnumerateFiles(projectDirectory, "*.xproj");

            foreach (var xprojFile in xprojFiles)
            {
                File.Delete(xprojFile);
            }
        }

        private void OutputDiagnostics(MigratedBuildComparisonData comparisonData)
        {
            OutputDiagnostics(comparisonData.MSBuildBuildOutputs, comparisonData.ProjectJsonBuildOutputs);
        }

        private void OutputDiagnostics(HashSet<string> msbuildBuildOutputs, HashSet<string> projectJsonBuildOutputs)
        {
            Console.WriteLine("Project.json Outputs:");

            Console.WriteLine(string.Join("\n", projectJsonBuildOutputs));

            Console.WriteLine("");

            Console.WriteLine("MSBuild Outputs:");

            Console.WriteLine(string.Join("\n", msbuildBuildOutputs));
        }

        private class MigratedBuildComparisonData
        {
            public HashSet<string> ProjectJsonBuildOutputs { get; }

            public HashSet<string> MSBuildBuildOutputs { get; }

            public MigratedBuildComparisonData(HashSet<string> projectJsonBuildOutputs,
                HashSet<string> msBuildBuildOutputs)
            {
                ProjectJsonBuildOutputs = projectJsonBuildOutputs;

                MSBuildBuildOutputs = msBuildBuildOutputs;
            }
        }

        private void WriteGlobalJson(string globalDirectory)  
        {  
            var file = Path.Combine(globalDirectory, "global.json");  
            File.WriteAllText(file, @"  
            {  
                ""projects"": [ ]  
            }");  
        }
    }
}
