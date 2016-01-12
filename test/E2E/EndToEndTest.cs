﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.Tools.Test.Utilities;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.DotNet.Tests.EndToEnd
{
    public class EndToEndTest : TestBase
    {
        private static readonly string s_expectedOutput = "Hello World!" + Environment.NewLine;
        private static readonly string s_testdirName = "e2etestroot";
        private static readonly string s_outputdirName = "testbin";
        
        private string Rid { get; set; }
        private string TestDirectory { get; set; }
        private string TestProject { get; set; }
        private string OutputDirectory { get; set; }

        public static void Main()
        {
            Console.WriteLine("Dummy Entrypoint.");
        }
       
        public EndToEndTest()
        {
            TestSetup();

            Rid = PlatformServices.Default.Runtime.GetLegacyRestoreRuntimeIdentifier();
        }

        [Fact]
        public void TestDotnetBuild()
        {
            var buildCommand = new BuildCommand(TestProject, output: OutputDirectory);

            buildCommand.Execute().Should().Pass();

            TestOutputExecutable(OutputDirectory, buildCommand.GetOutputExecutableName());
        }

        [Fact]
        public void TestDotnetIncrementalBuild()
        {
            TestSetup();

            // first build
            var buildCommand = new BuildCommand(TestProject, output: OutputDirectory);
            buildCommand.Execute().Should().Pass();
            TestOutputExecutable(OutputDirectory, buildCommand.GetOutputExecutableName());

            var latestWriteTimeFirstBuild = GetLastWriteTimeOfDirectoryFiles(OutputDirectory);

            // second build; should get skipped (incremental because no inputs changed)
            buildCommand.Execute().Should().Pass();
            TestOutputExecutable(OutputDirectory, buildCommand.GetOutputExecutableName());

            var latestWriteTimeSecondBuild = GetLastWriteTimeOfDirectoryFiles(OutputDirectory);
            Assert.Equal(latestWriteTimeFirstBuild, latestWriteTimeSecondBuild);

            TouchSourceFileInDirectory(TestDirectory);

            // third build; should get compiled because the source file got touched
            buildCommand.Execute().Should().Pass();
            TestOutputExecutable(OutputDirectory, buildCommand.GetOutputExecutableName());

            var latestWriteTimeThirdBuild = GetLastWriteTimeOfDirectoryFiles(OutputDirectory);
            Assert.NotEqual(latestWriteTimeSecondBuild, latestWriteTimeThirdBuild);
        }

        [Fact]
        public void TestDotnetForceIncrementalUnsafe()
        {
            TestSetup();

            // first build
            var buildCommand = new BuildCommand(TestProject, output: OutputDirectory);
            buildCommand.Execute().Should().Pass();
            TestOutputExecutable(OutputDirectory, buildCommand.GetOutputExecutableName());

            var latestWriteTimeFirstBuild = GetLastWriteTimeOfDirectoryFiles(OutputDirectory);

            // second build; will get recompiled due to force unsafe flag
            buildCommand = new BuildCommand(TestProject, output: OutputDirectory, forceIncrementalUnsafe:true);
            buildCommand.Execute().Should().Pass();
            TestOutputExecutable(OutputDirectory, buildCommand.GetOutputExecutableName());

            var latestWriteTimeSecondBuild = GetLastWriteTimeOfDirectoryFiles(OutputDirectory);
            Assert.NotEqual(latestWriteTimeFirstBuild, latestWriteTimeSecondBuild);
        }

        [Fact]
        public void TestDotnetIncrementalBuildDeleteOutputFile()
        {
            TestSetup();

            // first build
            var buildCommand = new BuildCommand(TestProject, output: OutputDirectory);
            buildCommand.Execute().Should().Pass();
            TestOutputExecutable(OutputDirectory, buildCommand.GetOutputExecutableName());

            var latestWriteTimeFirstBuild = GetLastWriteTimeOfDirectoryFiles(OutputDirectory);

            Reporter.Verbose.WriteLine($"Files in {OutputDirectory}");
            foreach (var file in Directory.EnumerateFiles(OutputDirectory))
            {
                Reporter.Verbose.Write($"\t {file}");
            }

            // delete output files
            foreach (var outputFile in Directory.EnumerateFiles(OutputDirectory).Where(f => Path.GetFileName(f).StartsWith(s_testdirName, StringComparison.OrdinalIgnoreCase)))
            {
                Reporter.Verbose.WriteLine($"Delete {outputFile}");

                File.Delete(outputFile);
                Assert.False(File.Exists(outputFile));
            }

            // second build; should get rebuilt since we deleted output items
            buildCommand.Execute().Should().Pass();
            TestOutputExecutable(OutputDirectory, buildCommand.GetOutputExecutableName());

            var latestWriteTimeSecondBuild = GetLastWriteTimeOfDirectoryFiles(OutputDirectory);
            Assert.NotEqual(latestWriteTimeFirstBuild, latestWriteTimeSecondBuild);
        }

        [Fact]
        [ActiveIssue(712, PlatformID.Windows | PlatformID.OSX | PlatformID.Linux)]
        public void TestDotnetBuildNativeRyuJit()
        {
            if(IsCentOS())
            {
                Console.WriteLine("Skipping native compilation tests on CentOS - https://github.com/dotnet/cli/issues/453");
                return;
            }

            var buildCommand = new BuildCommand(TestProject, output: OutputDirectory, native: true);

            buildCommand.Execute().Should().Pass();

            var nativeOut = Path.Combine(OutputDirectory, "native");
            TestOutputExecutable(nativeOut, buildCommand.GetOutputExecutableName());
        }

        [Fact]
        public void TestDotnetBuildNativeCpp()
        {
            if(IsCentOS())
            {
                Console.WriteLine("Skipping native compilation tests on CentOS - https://github.com/dotnet/cli/issues/453");
                return;
            }

            var buildCommand = new BuildCommand(TestProject, output: OutputDirectory, native: true, nativeCppMode: true);

            buildCommand.Execute().Should().Pass();

            var nativeOut = Path.Combine(OutputDirectory, "native");
            TestOutputExecutable(nativeOut, buildCommand.GetOutputExecutableName());
        }

        [Fact]
        public void TestDotnetCompileNativeCppIncremental()
        {
            if (IsCentOS())
            {
                Console.WriteLine("Skipping native compilation tests on CentOS - https://github.com/dotnet/cli/issues/453");
                return;
            }

            var nativeOut = Path.Combine(OutputDirectory, "native");

            // first build
            var buildCommand = new BuildCommand(TestProject, output: OutputDirectory, native: true, nativeCppMode: true);
            buildCommand.Execute().Should().Pass();
            TestOutputExecutable(nativeOut, buildCommand.GetOutputExecutableName());

            var latestWriteTimeFirstBuild = GetLastWriteTimeOfDirectoryFiles(OutputDirectory);

            // second build; should be skipped because nothing changed
            buildCommand.Execute().Should().Pass();
            TestOutputExecutable(nativeOut, buildCommand.GetOutputExecutableName());

            var latestWriteTimeSecondBuild = GetLastWriteTimeOfDirectoryFiles(OutputDirectory);
            Assert.Equal(latestWriteTimeFirstBuild, latestWriteTimeSecondBuild);
        }

        [Fact]
        public void TestDotnetRun()
        {
            var runCommand = new RunCommand(TestProject);

            runCommand.Execute()
                .Should()
                .Pass();
        }
        
        [Fact]
        public void TestDotnetPack()
        {
            var packCommand = new PackCommand(TestDirectory, output: OutputDirectory);

            packCommand.Execute()
                .Should()
                .Pass();
        }

        [Fact]
        public void TestDotnetPublish()
        {
            var publishCommand = new PublishCommand(TestProject, output: OutputDirectory);
            publishCommand.Execute().Should().Pass();

            TestOutputExecutable(OutputDirectory, publishCommand.GetOutputExecutable());    
        }

        private void TestSetup()
        {
            var root = Temp.CreateDirectory();

            TestDirectory = root.CreateDirectory(s_testdirName).Path;
            TestProject = Path.Combine(TestDirectory, "project.json");
            OutputDirectory = Path.Combine(TestDirectory, s_outputdirName);

            InitializeTestDirectory();   
        }

        private void InitializeTestDirectory()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(TestDirectory);

            new NewCommand().Execute().Should().Pass();
            new RestoreCommand().Execute("--quiet").Should().Pass();

            Directory.SetCurrentDirectory(currentDirectory);
        }

        private void TestOutputExecutable(string outputDir, string executableName)
        {
            var executablePath = Path.Combine(outputDir, executableName);

            var executableCommand = new TestCommand(executablePath);

            var result = executableCommand.ExecuteWithCapturedOutput("");

            result.Should().HaveStdOut(s_expectedOutput);
            result.Should().NotHaveStdErr();
            result.Should().Pass();
        }

        private bool IsCentOS()
        {
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                const string OSIDFILE = "/etc/os-release";

                if(File.Exists(OSIDFILE))
                {
                    return File.ReadAllText(OSIDFILE).ToLower().Contains("centos");
                }
            }

            return false;
        }

        private static DateTime GetLastWriteTimeOfDirectoryFiles(string outputDirectory)
        {
            return Directory.EnumerateFiles(outputDirectory).Max(f => File.GetLastWriteTime(f));
        }

        private static void TouchSourceFileInDirectory(string directory)
        {
            var csFile = Directory.EnumerateFiles(directory).First(f => Path.GetExtension(f).Equals(".cs"));
            File.SetLastWriteTimeUtc(csFile, DateTime.UtcNow);
        }
    }
}