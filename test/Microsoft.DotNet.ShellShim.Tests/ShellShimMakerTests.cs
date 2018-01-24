// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using FluentAssertions;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.TestFramework;
using Microsoft.DotNet.Tools.Test.Utilities;
using Microsoft.DotNet.Tools.Test.Utilities.Mock;
using Microsoft.DotNet.Tools.Tests.ComponentMocks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.ShellShim.Tests
{
    public class ShellShimMakerTests : TestBase
    {
        private readonly ITestOutputHelper _output;

        public ShellShimMakerTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData("my_native_app.exe", null)]
        [InlineData("./my_native_app.js", "nodejs")]
        [InlineData(@"C:\tools\my_native_app.dll", "dotnet")]
        public void GivenAnRunnerOrEntryPointItCanCreateConfig(string entryPoint, string runner)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            var shellShimMaker = new ShellShimMaker(TempRoot.Root);

            var tmpFile = Path.Combine(TempRoot.Root, Path.GetRandomFileName());

            shellShimMaker.CreateConfigFile(tmpFile, entryPoint, runner);

            new FileInfo(tmpFile).Should().Exist();

            var generated = XDocument.Load(tmpFile);

            generated.Descendants("appSettings")
                .Descendants("add")
                .Should()
                .Contain(e => e.Attribute("key").Value == "runner" && e.Attribute("value").Value == (runner ?? string.Empty))
                .And
                .Contain(e => e.Attribute("key").Value == "entryPoint" && e.Attribute("value").Value == entryPoint);
        }

        [Fact]
        public void GivenAnExecutablePathItCanGenerateShimFile()
        {
            var outputDll = MakeHelloWorldExecutableDll();

            var shellShimMaker = new ShellShimMaker(TempRoot.Root);
            var shellCommandName = nameof(ShellShimMakerTests) + Path.GetRandomFileName();

            shellShimMaker.CreateShim(
                outputDll.FullName,
                shellCommandName);
            var stdOut = ExecuteInShell(shellCommandName);

            stdOut.Should().Contain("Hello World");
        }

        [Theory]
        [InlineData("arg1 arg2", new[] { "arg1", "arg2" })]
        [InlineData(" \"arg1 with space\" arg2", new[] { "arg1 with space", "arg2" })]
        [InlineData(" \"arg with ' quote\" ", new[] { "arg with ' quote" })]
        public void GivenAShimItPassesThroughArguments(string arguments, string[] expectedPassThru)
        {
            var outputDll = MakeHelloWorldExecutableDll();

            var shellShimMaker = new ShellShimMaker(TempRoot.Root);
            var shellCommandName = nameof(ShellShimMakerTests) + Path.GetRandomFileName();

            shellShimMaker.CreateShim(
                outputDll.FullName,
                shellCommandName);

            var stdOut = ExecuteInShell(shellCommandName, arguments);

            for (int i = 0; i < expectedPassThru.Length; i++)
            {
                stdOut.Should().Contain($"{i} = {expectedPassThru[i]}");
            }
        }

        [InlineData(false)]
        [InlineData(true)]
        public void GivenAnExecutablePathWithExistingSameNameShimItThrows(bool testMockBehaviorIsInSync)
        {
            var shellCommandName = nameof(ShellShimMakerTests) + Path.GetRandomFileName();

            MakeNameConflictingCommand(TempRoot.Root, shellCommandName);

            IShellShimMaker shellShimMaker;
            if (testMockBehaviorIsInSync)
            {
                shellShimMaker = new ShellShimMakerMock(TempRoot.Root);
            }
            else
            {
                shellShimMaker = new ShellShimMaker(TempRoot.Root);
            }

            Action a = () => shellShimMaker.EnsureCommandNameUniqueness(shellCommandName);
            a.ShouldThrow<GracefulException>()
                .And.Message
                .Should().Contain(
                    $"Failed to install tool {shellCommandName}. A command with the same name already exists.");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GivenAnExecutablePathWithoutExistingSameNameShimItShouldNotThrow(bool testMockBehaviorIsInSync)
        {
            var shellCommandName = nameof(ShellShimMakerTests) + Path.GetRandomFileName();

            IShellShimMaker shellShimMaker;
            if (testMockBehaviorIsInSync)
            {
                shellShimMaker = new ShellShimMakerMock(TempRoot.Root);
            }
            else
            {
                shellShimMaker = new ShellShimMaker(TempRoot.Root);
            }

            Action a = () => shellShimMaker.EnsureCommandNameUniqueness(shellCommandName);
            a.ShouldNotThrow();
        }

        private static void MakeNameConflictingCommand(string pathToPlaceShim, string shellCommandName)
        {
            File.WriteAllText(Path.Combine(pathToPlaceShim, shellCommandName), string.Empty);
        }

        private string ExecuteInShell(string shellCommandName, string arguments = "")
        {
            ProcessStartInfo processStartInfo;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var file = Path.Combine(TempRoot.Root, shellCommandName + ".exe");
                processStartInfo = new ProcessStartInfo
                {
                    FileName = file,
                    UseShellExecute = false,
                    Arguments = arguments,
                };
            }
            else
            {
                processStartInfo = new ProcessStartInfo
                {
                    FileName = "sh",
                    Arguments = shellCommandName + " " + arguments,
                    UseShellExecute = false
                };
            }

            _output.WriteLine($"Launching '{processStartInfo.FileName} {processStartInfo.Arguments}'");
            processStartInfo.WorkingDirectory = TempRoot.Root;
            processStartInfo.EnvironmentVariables["PATH"] = Path.GetDirectoryName(new Muxer().MuxerPath);

            processStartInfo.ExecuteAndCaptureOutput(out var stdOut, out var stdErr);

            stdErr.Should().BeEmpty();

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
    }
}
