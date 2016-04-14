﻿using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.TestFramework;
using Microsoft.DotNet.Tools.Test.Utilities;
using Microsoft.Extensions.PlatformAbstractions;
using Xunit;

namespace Microsoft.DotNet.Tools.Publish.Tests
{
    public class PublishDesktopTests : TestBase
    {
        [WindowsOnlyTheory]
        [InlineData(null, null)]
        [InlineData("win7-x64", "the-win-x64-version.txt")]
        [InlineData("win7-x86", "the-win-x86-version.txt")]
        public async Task DesktopApp_WithDependencyOnNativePackage_ProducesExpectedOutput(string runtime, string expectedOutputName)
        {
            if(string.IsNullOrEmpty(expectedOutputName))
            {
                expectedOutputName = $"the-win-{RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant()}-version.txt";
            }

            var testInstance = TestAssetsManager.CreateTestInstance(Path.Combine("..", "DesktopTestProjects", "DesktopAppWithNativeDep"))
                .WithLockFiles();

            var publishCommand = new PublishCommand(testInstance.TestRoot, runtime: runtime);
            var result = await publishCommand.ExecuteAsync();

            result.Should().Pass();

            // Test the output
            var outputDir = publishCommand.GetOutputDirectory(portable: false);
            outputDir.Should().HaveFile(expectedOutputName);
            outputDir.Should().HaveFile(publishCommand.GetOutputExecutable());
        }

        [WindowsOnlyTheory]
        [InlineData("KestrelDesktopWithRuntimes", "http://localhost:20201", null, "libuv.dll", false)]
        [InlineData("KestrelDesktopWithRuntimes", "http://localhost:20202", "win7-x64", "libuv.dll", false)]
        [InlineData("KestrelDesktopWithRuntimes", "http://localhost:20203", "win7-x86", "libuv.dll", false)]
        [InlineData("KestrelDesktopForce32", "http://localhost:20204", "win7-x86", "libuv.dll", true)]
        [InlineData("KestrelDesktop", "http://localhost:20205", null, "libuv.dll", false)]
        [InlineData("KestrelDesktop", "http://localhost:20206", "win7-x64", "libuv.dll", false)]
        [InlineData("KestrelDesktop", "http://localhost:20207", "win7-x86", "libuv.dll", false)]
        public async Task DesktopApp_WithKestrel_WorksWhenPublished(string project, string url, string runtime, string libuvName, bool forceRunnable)
        {
            var runnable = forceRunnable || string.IsNullOrEmpty(runtime) || (PlatformServices.Default.Runtime.GetLegacyRestoreRuntimeIdentifier().Contains(runtime));

            var testInstance = GetTestInstance()
                .WithLockFiles();

            var publishCommand = new PublishCommand(Path.Combine(testInstance.TestRoot, project), runtime: runtime);
            var result = await publishCommand.ExecuteAsync();

            result.Should().Pass();

            // Test the output
            var outputDir = publishCommand.GetOutputDirectory(portable: false);
            outputDir.Should().HaveFile(libuvName);
            outputDir.Should().HaveFile(publishCommand.GetOutputExecutable());

            Task exec = null;
            if (runnable)
            {
                var outputExePath = Path.Combine(outputDir.FullName, publishCommand.GetOutputExecutable());

                var command = new TestCommand(outputExePath);
                try
                {
                    exec = command.ExecuteAsync(url);
                    NetworkHelper.IsServerUp(url).Should().BeTrue($"Unable to connect to kestrel server - {project} @ {url}");
                    NetworkHelper.TestGetRequest(url, url);
                }
                finally
                {
                    command.KillTree();
                }
                if (exec != null)
                {
                    await exec;
                }
            }
        }

        [WindowsOnlyTheory]
        [InlineData("KestrelDesktop", "http://localhost:20301")]
        [InlineData("KestrelDesktopWithRuntimes", "http://localhost:20302")]
        public async Task DesktopApp_WithKestrel_WorksWhenRun(string project, string url)
        {
            // Disabled due to https://github.com/dotnet/cli/issues/2428
            if (RuntimeInformation.ProcessArchitecture == Architecture.X86)
            {
                return;
            }

            var testInstance = GetTestInstance()
                .WithLockFiles()
                .WithBuildArtifacts();

            Task exec = null;
            var command = new RunCommand(Path.Combine(testInstance.TestRoot, project));
            try
            {
                exec = command.ExecuteAsync(url);
                NetworkHelper.IsServerUp(url).Should().BeTrue($"Unable to connect to kestrel server - {project} @ {url}");
                NetworkHelper.TestGetRequest(url, url);
            }
            finally
            {
                command.KillTree();
            }
            if (exec != null)
            {
                await exec;
            }
        }

        private static TestInstance GetTestInstance([CallerMemberName] string callingMethod = "")
        {
            return TestAssetsManager.CreateTestInstance(Path.Combine("..", "DesktopTestProjects", "DesktopKestrelSample"), callingMethod);
        }
    }
}
