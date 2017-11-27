// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.TestFramework;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;

namespace Microsoft.DotNet.ShellShimMaker.Tests
{
    public class ShellShimMakerTests : TestBase
    {
        private readonly string _pathToPlaceShim;

        public ShellShimMakerTests()
        {
            _pathToPlaceShim = Path.GetTempPath();
        }

        [Fact]
        public void GivenAnExecutablePathItCanGenerateShimFile()
        {
            var outputDll = MakeHelloWorldExecutableDll();

            var shellShimMaker = new ShellShimMaker(_pathToPlaceShim);
            var shellCommandName = nameof(ShellShimMakerTests) + Path.GetRandomFileName();

            shellShimMaker.CreateShim(
                outputDll.FullName,
                shellCommandName);
            var stdOut = ExecuteInShell(shellCommandName);

            stdOut.Should().Contain("Hello World");

            // Tear down
            shellShimMaker.Remove(shellCommandName);
        }

        [Fact]
        public void GivenAnExecutablePathWithExistingSameNameShimItThrows()
        {
            var shellCommandName = nameof(ShellShimMakerTests) + Path.GetRandomFileName();

            MakeNameConflictingCommand(_pathToPlaceShim, shellCommandName);

            var shellShimMaker = new ShellShimMaker(_pathToPlaceShim);

            Action a = () => shellShimMaker.EnsureCommandNameUniqueness(shellCommandName);
            a.ShouldThrow<GracefulException>()
                .And.Message
                .Should().Contain($"Failed to create tool {shellCommandName}, a command with the same name existed");

            // Tear down
            shellShimMaker.Remove(shellCommandName);
        }


        [Fact]
        public void GivenAnExecutablePathWithoutExistingSameNameShimItShouldNotThrow()
        {
            var shellCommandName = nameof(ShellShimMakerTests) + Path.GetRandomFileName();

            var shellShimMaker = new ShellShimMaker(_pathToPlaceShim);

            Action a = () => shellShimMaker.EnsureCommandNameUniqueness(shellCommandName);
            a.ShouldNotThrow();

            // Tear down
            shellShimMaker.Remove(shellCommandName);
        }

        private static void MakeNameConflictingCommand(string pathToPlaceShim, string shellCommandName)
        {
            File.WriteAllText(Path.Combine(pathToPlaceShim, shellCommandName), string.Empty);
        }

        private string ExecuteInShell(string shellCommandName)
        {
            string stdOut;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "CMD.exe",
                    Arguments = $"/C {shellCommandName}",
                    UseShellExecute = false
                };

                processStartInfo.EnvironmentVariables["PATH"] = Path.GetDirectoryName(new Muxer().MuxerPath);

                ExecuteAndCaptureOutputWithAssert(processStartInfo, out stdOut);
            }
            else
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "sh",
                    Arguments = shellCommandName,
                    UseShellExecute = false
                };

                processStartInfo.EnvironmentVariables["PATH"] = Path.GetDirectoryName(new Muxer().MuxerPath);

                ExecuteAndCaptureOutputWithAssert(processStartInfo, out stdOut);
            }

            return stdOut ?? "";
        }

        private static FileInfo MakeHelloWorldExecutableDll()
        {
            const string testAppName = "TestAppSimple";
            const string emptySpaceToTestSpaceInPath = " ";
            TestAssetInstance testInstance = TestAssets.Get(testAppName)
                .CreateInstance(testAppName + emptySpaceToTestSpaceInPath)
                .UseCurrentRuntimeFrameworkVersion()
                .WithRestoreFiles()
                .WithBuildFiles();

            var configuration = Environment.GetEnvironmentVariable("CONFIGURATION") ?? "Debug";

            FileInfo outputDll = testInstance.Root.GetDirectory("bin", configuration)
                .GetDirectories().Single()
                .GetFile($"{testAppName}.dll");

            return outputDll;
        }

        private void ExecuteAndCaptureOutputWithAssert(ProcessStartInfo startInfo, out string stdOut)
        {
            StreamForwarder outStream = new StreamForwarder().Capture();
            StreamForwarder errStream = new StreamForwarder().Capture();

            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            startInfo.WorkingDirectory = _pathToPlaceShim;

            var process = new Process
            {
                StartInfo = startInfo,
            };

            process.EnableRaisingEvents = true;

            process.Start();

            Task taskOut = outStream.BeginRead(process.StandardOutput);
            Task taskErr = errStream.BeginRead(process.StandardError);

            process.WaitForExit();

            taskOut.Wait();
            taskErr.Wait();

            stdOut = outStream.CapturedOutput;
            var stdErr = errStream.CapturedOutput;

            stdErr.Should().BeEmpty("Arguments: " + startInfo.Arguments + " WorkingDirectory: "+ startInfo.WorkingDirectory);
            process.ExitCode.Should().Be(0);
        }
    }
}
