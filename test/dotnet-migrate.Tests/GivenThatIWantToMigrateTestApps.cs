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
        [InlineData("PJTestAppWithRuntimeOptions")]
        [InlineData("PJTestAppWithContents")]
        [InlineData("PJAppWithAssemblyInfo")]
        [InlineData("PJTestAppWithEmbeddedResources")]
        public void ItMigratesApps(string projectName)
        {
            var projectDirectory = TestAssets.GetPJ(projectName)
                .CreateInstance()
                .WithSourceFiles()
                .WithRestoreFiles()
                .Root;

            var outputComparisonData = BuildProjectJsonMigrateBuildMSBuild(projectDirectory, projectName);

            var outputsIdentical =
                outputComparisonData.ProjectJsonBuildOutputs.SetEquals(outputComparisonData.MSBuildBuildOutputs);

            if (!outputsIdentical)
            {
                OutputDiagnostics(outputComparisonData);
            }

            outputsIdentical.Should().BeTrue();

            VerifyAllMSBuildOutputsRunnable(projectDirectory);

            var outputCsProj = projectDirectory.GetFile(projectName + ".csproj");
            var csproj = outputCsProj.OpenText().ReadToEnd();
            csproj.EndsWith("\n").Should().Be(true);
        }

        [Fact]
        public void ItMigratesSignedApps()
        {
            var projectName = "PJTestAppWithSigning";
            var projectDirectory = TestAssets.GetPJ(projectName)
                .CreateInstance()
                .WithSourceFiles()
                .WithRestoreFiles()
                .Root;

            var outputComparisonData = BuildProjectJsonMigrateBuildMSBuild(projectDirectory, projectName);

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
            var projectDirectory = TestAssets.GetPJ("PJConsoleTemplate")
                .CreateInstance()
                .WithSourceFiles()
                .Root;

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
            var projectDirectory = TestAssets.GetPJ("PJWebTemplate")
                .CreateInstance()
                .WithSourceFiles()
                .WithRestoreFiles()
                .Root;

            var globalDirectory = projectDirectory.Parent;

            var projectJsonFile = projectDirectory.GetFile("project.json");  

            WriteGlobalJson(globalDirectory);

            var outputComparisonData = GetComparisonData(projectDirectory);

            var outputsIdentical = outputComparisonData
                .ProjectJsonBuildOutputs
                .SetEquals(outputComparisonData.MSBuildBuildOutputs);

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

            var projectDirectory = testInstance.Root;

            MigrateProject(new [] { projectDirectory.FullName });

            Restore(projectDirectory);
            PublishMSBuild(projectDirectory, projectName);
        }

        [Fact]
        public void ItAddsMicrosoftNetWebSdkToTheSdkAttributeOfAWebApp()
        {
            var projectDirectory = TestAssets.GetPJ("ProjectJsonWebTemplate")
                .CreateInstance()
                .WithSourceFiles()
                .WithRestoreFiles()
                .Root;

            var globalDirectory = projectDirectory.Parent;  
            var projectJsonFile = projectDirectory.GetFile("project.json");  
              
            MigrateProject(projectDirectory );

            var csProj = projectDirectory.GetFile(projectDirectory.Name + ".csproj");

            csProj.ReadAllText().Should().Contain(@"Sdk=""Microsoft.NET.Sdk.Web""");
        }

        [Theory]
        [InlineData("PJTestLibraryWithTwoFrameworks")]
        public void ItMigratesProjectsWithMultipleTFMs(string projectName)
        {
            var projectDirectory = TestAssets.GetPJ(projectName)
                .CreateInstance(identifier: projectName)
                .WithSourceFiles()
                .WithRestoreFiles()
                .Root;

            var outputComparisonData = BuildProjectJsonMigrateBuildMSBuild(projectDirectory, projectName);

            var outputsIdentical = outputComparisonData
                .ProjectJsonBuildOutputs
                .SetEquals(outputComparisonData.MSBuildBuildOutputs);

            if (!outputsIdentical)
            {
                OutputDiagnostics(outputComparisonData);
            }

            outputsIdentical.Should().BeTrue();
        }

        [Theory]
        [InlineData("PJTestAppWithLibrary/TestLibrary")]
        [InlineData("PJTestLibraryWithAnalyzer")]
        [InlineData("PJTestLibraryWithConfiguration")]
        public void ItMigratesALibrary(string projectName)
        {
            var projectDirectory = TestAssets.GetPJ(projectName)
                .CreateInstance(identifier: projectName)
                .WithSourceFiles()
                .WithRestoreFiles()
                .Root;

            var outputComparisonData = BuildProjectJsonMigrateBuildMSBuild(
                projectDirectory, 
                Path.GetFileNameWithoutExtension(projectName));

            var outputsIdentical = outputComparisonData
                .ProjectJsonBuildOutputs
                .SetEquals(outputComparisonData.MSBuildBuildOutputs);

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
            var projectDirectory = TestAssets.GetPJ("PJTestAppDependencyGraph")
                .CreateInstance(identifier: $"{projectName}.RefsTest")
                .WithSourceFiles()
                .Root;

            MigrateProject(projectDirectory.GetDirectory(projectName));

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
            var projectDirectory = TestAssets.GetPJ("PJTestAppDependencyGraph")
                .CreateInstance(identifier: $"{projectName}.SkipRefsTest")
                .WithSourceFiles()
                .Root;

            MigrateProject(new [] { projectDirectory.GetFile(projectName).FullName, "--skip-project-references" });

            VerifyMigration(Enumerable.Repeat(projectName, 1), projectDirectory);
         }

         [Theory]
         [InlineData(true)]
         [InlineData(false)]
         public void ItMigratesAllProjectsInGivenDirectory(bool skipRefs)
         {
            var projectDirectory = TestAssets.GetPJ("PJTestAppDependencyGraph")
                .CreateInstance(identifier: $"MigrateDirectory.SkipRefs.{skipRefs}")
                .WithSourceFiles()
                .Root;

            if (skipRefs)
            {
                MigrateProject(projectDirectory.FullName, "--skip-project-references" );
            }
            else
            {
                MigrateProject(projectDirectory);
            }

            string[] migratedProjects = new string[] { "ProjectA", "ProjectB", "ProjectC", "ProjectD", "ProjectE", "ProjectF", "ProjectG", "ProjectH", "ProjectI", "ProjectJ" };

            VerifyMigration(migratedProjects, projectDirectory);
         }

         [Fact]
         public void ItMigratesGivenProjectJson()
         {
            var projectDirectory = TestAssets.GetPJ("PJTestAppDependencyGraph")
                .CreateInstance()
                .WithSourceFiles()
                .Root;

            var project = projectDirectory
                .GetDirectory("ProjectA")
                .GetFile("project.json");

            MigrateProject(project);

            string[] migratedProjects = new string[] { "ProjectA", "ProjectB", "ProjectC", "ProjectD", "ProjectE" };

            VerifyMigration(migratedProjects, projectDirectory);
         }

         [Fact]
         // regression test for https://github.com/dotnet/cli/issues/4269
         public void ItMigratesAndBuildsP2PReferences()
         {
            var assetsDir = TestAssets.GetPJ("PJTestAppDependencyGraph")
                .CreateInstance()
                .WithSourceFiles()
                .WithRestoreFiles()
                .Root;

            var projectDirectory = assetsDir.GetDirectory("ProjectF");

            var restoreDirectories = new DirectoryInfo[]
            {
                projectDirectory, 
                assetsDir.GetDirectory("ProjectG")
            };

            var outputComparisonData = BuildProjectJsonMigrateBuildMSBuild(projectDirectory, "ProjectF", new [] { projectDirectory.FullName }, restoreDirectories);

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
            var assetsDir = TestAssets.GetPJ(Path.Combine("PJTestAppDependencyGraph", "ProjectsWithGlobalJson"))
                .CreateInstance(identifier: $"ProjectsWithGlobalJson.{projectName}")
                .WithSourceFiles()
                .WithRestoreFiles()
                .Root;

            var globalJson = assetsDir.GetFile("global.json");

            var restoreDirectories = new DirectoryInfo[]
            {
                assetsDir.GetDirectory("src", "ProjectH"),
                assetsDir.GetDirectory("src", "ProjectI"),
                assetsDir.GetDirectory("src with spaces", "ProjectJ")
            };

            var projectDirectory = assetsDir.GetDirectory(path, projectName);

            var outputComparisonData = BuildProjectJsonMigrateBuildMSBuild(
                projectDirectory, 
                projectName,
                new [] { globalJson.FullName },
                restoreDirectories);

            var outputsIdentical = outputComparisonData
                .ProjectJsonBuildOutputs
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
            var projectDirectory = TestAssets
                .CreateTestDirectory("Migration_outputs_error_when_no_projects_found");

            string argstr = string.Empty;

            string errorMessage = string.Empty;

            if (useGlobalJson)
            {
                var globalJson = projectDirectory.GetFile("global.json");

                using (var fileStream = globalJson.OpenWrite())
                {
                    using (var streamWriter = new StreamWriter(fileStream))
                    {
                        streamWriter.WriteLine("{");
                        streamWriter.WriteLine("\"projects\": [ \".\" ]");
                        streamWriter.WriteLine("}");
                    }
                }

                argstr = globalJson.FullName;

                errorMessage = "Unable to find any projects in global.json";
            }
            else
            {
                argstr = projectDirectory.FullName;

                errorMessage = $"No project.json file found in '{projectDirectory.FullName}'";
            }

            var result = new TestCommand("dotnet")
                .WithWorkingDirectory(projectDirectory)
                .ExecuteWithCapturedOutput($"migrate {argstr}");

            // Expecting an error exit code.
            result.ExitCode.Should().Be(1);

            // Verify the error messages. Note that debug builds also show the call stack, so we search
            // for the error strings that should be present (rather than an exact match).
            result.StdErr.Should().Contain(errorMessage)
                     .And.Contain("Migration failed.");
        }

        [Fact]
        public void ItMigratesAndPublishesProjectsWithRuntimes()
        {
            var projectName = "PJTestAppSimple";
            var projectDirectory = TestAssets.GetPJ(projectName)
                .CreateInstance()
                .WithSourceFiles()
                .WithRestoreFiles()
                .Root;

            CleanBinObj(projectDirectory);
            BuildProjectJsonMigrateBuildMSBuild(projectDirectory, projectName);
            PublishMSBuild(projectDirectory, projectName, "win7-x64");
        }

        [WindowsOnlyTheory]
        [InlineData("DesktopTestProjects", "PJAutoAddDesktopReferencesDuringMigrate", true)]
        [InlineData("TestProjects", "PJTestAppSimple", false)]
        public void ItAutoAddDesktopReferencesDuringMigrate(string testGroup, string projectName, bool isDesktopApp)
        {
            var runtime = DotnetLegacyRuntimeIdentifiers.InferLegacyRestoreRuntimeIdentifier();

            var projectDirectory = TestAssets.GetPJ(testGroup, projectName)
                .CreateInstance()
                .WithSourceFiles()
                .WithRestoreFiles()
                .Root;
            
            MigrateProject(projectDirectory);
            
            Restore(projectDirectory, runtime: runtime);
            
            BuildMSBuild(projectDirectory, projectName, runtime:runtime);
            
            VerifyAutoInjectedDesktopReferences(projectDirectory, projectName, isDesktopApp);
            
            VerifyAllMSBuildOutputsRunnable(projectDirectory);
        }

        [Fact]
        public void ItBuildsAMigratedAppWithAnIndirectDependency()
        {
            var projectName = "ProjectA";

            var projectDirectory = TestAssets.GetPJ("PJTestAppDependencyGraph")
                .CreateInstance()
                .WithSourceFiles()
                .Root
                .GetDirectory(projectName);

            MigrateProject(projectDirectory);

            Restore(projectDirectory);

            BuildMSBuild(projectDirectory, projectName);

            VerifyAllMSBuildOutputsRunnable(projectDirectory);
        }

        [Fact]
        public void ItMigratesProjectWithOutputName()
        {
            var projectName = "PJAppWithOutputAssemblyName";
            var expectedOutputName = "MyApp";

            var projectDirectory = TestAssets.GetPJ("PJAppWithOutputAssemblyName")
                .CreateInstance()
                .WithSourceFiles()
                .WithRestoreFiles()
                .Root;
            
            var expectedCsprojFile = projectDirectory.GetFile($"{projectName}.csproj");

            MigrateProject(projectDirectory);

            expectedCsprojFile.Should().Exist();

            Restore(projectDirectory, projectName);

            BuildMSBuild(projectDirectory, projectName);

            projectDirectory
                .GetDirectory("bin", "Debug", "netcoreapp1.0")
                .GetFile($"{expectedOutputName}.pdb")
                .Should().Exist();
            
            PackMSBuild(projectDirectory, projectName);
            
            projectDirectory
                .GetDirectory("bin", "Debug")
                .GetFile($"{projectName}.1.0.0.nupkg")
                .Should().Exist();
        }

        [Theory]
        [InlineData("PJLibraryWithoutNetStandardLibRef")]
        [InlineData("PJLibraryWithNetStandardLibRef")]
        public void ItMigratesAndBuildsLibrary(string projectName)
        {
            var projectDirectory = TestAssets.GetPJ(projectName)
                .CreateInstance(identifier: projectName)
                .WithSourceFiles()
                .Root;

            MigrateProject(projectDirectory);

            Restore(projectDirectory, projectName);

            BuildMSBuild(projectDirectory, projectName);
        }

        [Fact]
        public void ItFailsGracefullyWhenMigratingAppWithMissingDependency()
        {
            var projectDirectory = TestAssets.Get(TestAssetKinds.NonRestoredTestProjects, "PJMigrateAppWithMissingDep")
                .CreateInstance()
                .WithSourceFiles()
                .Root
                .GetDirectory("MyApp");

            var migrationOutputFile = projectDirectory.GetFile("migration-output.json");

            MigrateCommand.Run(new string[] 
            { 
                projectDirectory.FullName, 
                "-r", 
                migrationOutputFile.FullName, 
                "--format-report-file-json" 
            }).Should().NotBe(0);

            migrationOutputFile.Should().Exist();

            migrationOutputFile.OpenText().ReadToEnd().Should().Contain("MIGRATE1018");
        }

        [Fact]
        public void ItMigratesSln()
        {
            var rootDirectory = TestAssets.Get("PJTestAppWithSlnAndMultipleProjects")
                .CreateInstance()
                .WithSourceFiles()
                .Root;

            var testAppProjectDirectory = rootDirectory.GetDirectory("TestApp");
            var testLibProjectDirectory = rootDirectory.GetDirectory("TestLibrary");
            var slnPath = testAppProjectDirectory.GetFile("TestApp.sln");

            MigrateProject(slnPath);

            Restore(testAppProjectDirectory, "TestApp.csproj");

            BuildMSBuild(testAppProjectDirectory, "TestApp.sln", "Release");
        }

        private void VerifyAutoInjectedDesktopReferences(DirectoryInfo projectDirectory, string projectName, bool shouldBePresent)
        {
            if (projectName != null)
            {
                projectName = projectName + ".csproj";
            }

            var root = ProjectRootElement.Open(projectDirectory.GetFile(projectName).FullName);

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

        private void VerifyMigration(IEnumerable<string> expectedProjects, DirectoryInfo rootDir)
         {
             var migratedProjects = rootDir
                .EnumerateFiles("*.csproj", SearchOption.AllDirectories)
                .Where(s => s.Directory.EnumerateFiles("*.csproj").Count() == 1)
                .Where(s => s.Directory.Name.Contains("Project"))
                .Select(s => s.Directory.Name);

             migratedProjects.Should().BeEquivalentTo(expectedProjects);
         }

        private MigratedBuildComparisonData GetComparisonData(DirectoryInfo projectDirectory)
        {
            File.Copy("NuGet.tempaspnetpatch.config", projectDirectory.GetFile("NuGet.Config").FullName);
            
            RestoreProjectJson(projectDirectory);

            var outputComparisonData =
                BuildProjectJsonMigrateBuildMSBuild(projectDirectory, projectDirectory.Name);

            return outputComparisonData;
        }

        private void VerifyAllMSBuildOutputsRunnable(DirectoryInfo projectDirectory)
        {
            var dllFileName = projectDirectory.Name + ".dll";

            var runnableDlls = projectDirectory
                .GetDirectory("bin")
                .EnumerateFiles(dllFileName, SearchOption.AllDirectories);

            foreach (var dll in runnableDlls)
            {
                new TestCommand("dotnet")
                    .ExecuteWithCapturedOutput($"\"{dll.FullName}\"")
                    .Should().Pass();
            }
        }

        private void VerifyAllMSBuildOutputsAreSigned(DirectoryInfo projectDirectory)
        {
            var dllFileName = projectDirectory.Name + ".dll";

            var runnableDlls = projectDirectory
                .GetDirectory("bin")
                .EnumerateFiles(dllFileName, SearchOption.AllDirectories);

            foreach (var dll in runnableDlls)
            {
                var assemblyName = AssemblyLoadContext.GetAssemblyName(dll.FullName);

                var token = assemblyName.GetPublicKeyToken();

                token.Should().NotBeNullOrEmpty();
            }
        }

        private MigratedBuildComparisonData BuildProjectJsonMigrateBuildMSBuild(DirectoryInfo projectDirectory, 
                                                                                string projectName)
        {
            return BuildProjectJsonMigrateBuildMSBuild(projectDirectory, 
                                                       projectName,
                                                       new [] { projectDirectory.FullName }, 
                                                       new [] { projectDirectory });
        }

        private MigratedBuildComparisonData BuildProjectJsonMigrateBuildMSBuild(DirectoryInfo projectDirectory, 
                                                                                string projectName,
                                                                                string[] migrateArgs,
                                                                                DirectoryInfo[] restoreDirectories)
        {
            BuildProjectJson(projectDirectory);

            var projectJsonBuildOutputs = new HashSet<string>(CollectBuildOutputs(projectDirectory));

            CleanBinObj(projectDirectory);

            // Remove lock file for migration
            foreach(var dir in restoreDirectories)
            {
                dir.GetFile("project.lock.json").Delete();
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

        private IEnumerable<string> CollectBuildOutputs(DirectoryInfo projectDirectory)
        {
            var fullBinPath = projectDirectory.GetDirectory("bin");

            return fullBinPath
                .EnumerateFiles("*", SearchOption.AllDirectories)
                .Select(p => p.FullName.Substring(fullBinPath.FullName.Length));
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

        private void CleanBinObj(DirectoryInfo projectDirectory)
        {
            var dirs = new DirectoryInfo[] 
            { 
                projectDirectory.GetDirectory("bin"), 
                projectDirectory.GetDirectory("obj") 
            };

            foreach (var dir in dirs)
            {
                if(dir.Exists)
                {
                    dir.Delete(true);
                }
            }
        }

        private void BuildProjectJson(DirectoryInfo projectDirectory)
        {
            var projectFile = projectDirectory.GetFile("project.json");
            
            var result = new BuildPJCommand()
                .WithCapturedOutput()
                .Execute($"\"{projectFile.FullName}\"");

            result.Should().Pass();
        }

        private void MigrateProject(params FileSystemInfo[] args)
        {
            MigrateProject(args.Select(f => f.FullName).ToArray());
        }

        private void MigrateProject(params string[] migrateArgs)
        {
            var result =
                MigrateCommand.Run(migrateArgs);

            result.Should().Be(0);
        }

        private void RestoreProjectJson(DirectoryInfo projectDirectory)
        {
            new TestCommand("dotnet")
                .WithWorkingDirectory(projectDirectory)
                .Execute("restore-projectjson")
                .Should().Pass();
        }

        private void Restore(DirectoryInfo projectDirectory, string projectName=null, string runtime=null)
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
            DirectoryInfo projectDirectory,
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
            DirectoryInfo projectDirectory,
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

        private string PackMSBuild(DirectoryInfo projectDirectory, string projectName)
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

        private void DeleteXproj(DirectoryInfo projectDirectory)
        {
            var xprojFiles = projectDirectory.EnumerateFiles("*.xproj");

            foreach (var xprojFile in xprojFiles)
            {
                xprojFile.Delete();
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

        private void WriteGlobalJson(DirectoryInfo globalDirectory)  
        {  
            var globalJson = globalDirectory.GetFile("global.json");
            
            using (var fileStream = globalJson.OpenWrite())
            {
                using (var streamWriter = new StreamWriter(fileStream))
                {
                    streamWriter.WriteLine("{");
                    streamWriter.WriteLine("\"projects\": [ ]");
                    streamWriter.WriteLine("}");
                }
            }
        }
    }
}
