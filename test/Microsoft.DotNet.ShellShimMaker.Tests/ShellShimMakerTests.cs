// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using FluentAssertions;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;

namespace Microsoft.DotNet.ShellShimMaker.Tests
{
    public class ShellShimMakerTests : TestBase
    {
        [Fact]
        public void GivenAnExecutablePathItCanGenerateShimFile()
        {
            var outputDll = MakeHelloWorldExecutableDll();

            var muxer = new Muxer();
            var shellShimMaker = new ShellShimMaker(Path.GetDirectoryName(muxer.MuxerPath));
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
            var muxer = new Muxer();
            var pathToPlaceShim = Path.GetDirectoryName(muxer.MuxerPath);
            var shellCommandName = nameof(ShellShimMakerTests) + Path.GetRandomFileName();

            MakeNameConflictingCommand(pathToPlaceShim, shellCommandName);

            var shellShimMaker = new ShellShimMaker(pathToPlaceShim);

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
            var muxer = new Muxer();
            var pathToPlaceShim = Path.GetDirectoryName(muxer.MuxerPath);
            var shellCommandName = nameof(ShellShimMakerTests) + Path.GetRandomFileName();

            var shellShimMaker = new ShellShimMaker(pathToPlaceShim);

            Action a = () => shellShimMaker.EnsureCommandNameUniqueness(shellCommandName);
            a.ShouldNotThrow();

            // Tear down
            shellShimMaker.Remove(shellCommandName);
        }

        private static void MakeNameConflictingCommand(string pathToPlaceShim, string shellCommandName)
        {
            File.WriteAllText(Path.Combine(pathToPlaceShim, shellCommandName), string.Empty);
        }


        private static string ExecuteInShell(string shellCommandName)
        {
            string stdOut;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ExecuteAndCaptureOutputWithAssert(new ProcessStartInfo
                {
                    FileName = "CMD.exe",
                    Arguments = $"/C {shellCommandName}",
                    UseShellExecute = false
                }, out stdOut);
            }
            else
            {
                ExecuteAndCaptureOutputWithAssert(new ProcessStartInfo
                {
                    FileName = "sh",
                    Arguments = $"-c {shellCommandName}",
                    UseShellExecute = false
                }, out stdOut);
            }

            return stdOut ?? "";
        }

        private static FileInfo MakeHelloWorldExecutableDll()
        {
            const string target = "netcoreapp2.1";
            const string testAppName = "TestAppSimple";
            const string emptySpaceToTestSpaceInPath = " ";
            var testInstance = TestAssets.Get(testAppName)
                .CreateInstance(testAppName + emptySpaceToTestSpaceInPath + target.Replace('.', '_'))
                .WithSourceFiles().WithRestoreFiles().WithBuildFiles();

            var configuration = Environment.GetEnvironmentVariable("CONFIGURATION") ?? "Debug";

            var outputDll = testInstance.Root.GetDirectory("bin", configuration, target)
                .GetFile($"{testAppName}.dll");
            return outputDll;
        }

        private static void ExecuteAndCaptureOutputWithAssert(ProcessStartInfo startInfo, out string stdOut)
        {
            var outStream = new StreamForwarder().Capture();
            var errStream = new StreamForwarder().Capture();

            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            var process = new Process
            {
                StartInfo = startInfo
            };

            process.EnableRaisingEvents = true;

            process.Start();

            var taskOut = outStream.BeginRead(process.StandardOutput);
            var taskErr = errStream.BeginRead(process.StandardError);

            process.WaitForExit();

            taskOut.Wait();
            taskErr.Wait();

            stdOut = outStream.CapturedOutput;
            var stdErr = errStream.CapturedOutput;

            stdErr.Should().BeEmpty();
            process.ExitCode.Should().Be(0);
        }
    }
}
