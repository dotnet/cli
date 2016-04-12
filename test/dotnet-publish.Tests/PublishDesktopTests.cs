using System.Diagnostics;
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
        [InlineData("KestrelDesktopWithRuntimes", "http://localhost:20201", null, "libuv.dll")]
        [InlineData("KestrelDesktopWithRuntimes", "http://localhost:20202", "win7-x64", "libuv.dll")]
        [InlineData("KestrelDesktopWithRuntimes", "http://localhost:20202", "win7-x86", "libuv.dll")]
        [InlineData("KestrelDesktop", "http://localhost:20204", null, "libuv.dll")]
        [InlineData("KestrelDesktop", "http://localhost:20205", "win7-x64", "libuv.dll")]
        [InlineData("KestrelDesktop", "http://localhost:20205", "win7-x86", "libuv.dll")]
        public async Task DesktopApp_WithKestrel_WorksWhenPublished(string project, string url, string runtime, string libuvName)
        {
            var runnable = string.IsNullOrEmpty(runtime) || PlatformServices.Default.Runtime.GetAllCandidateRuntimeIdentifiers().Contains(runtime);

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

                // If we're running on a 64-bit OS but trying to launch a 32-bit process, we need to use corflags to force
                // the process to be 32-bit. At some point we need to look at how users should do this themselves, but at
                // worst they can use corflags too for v1.
                if(RuntimeInformation.OSArchitecture == Architecture.X64 && runtime.EndsWith("x86"))
                {
                    Command.Create("corflags", new[] { outputExePath, "/32BITREQ+" })
                        .Execute()
                        .Should().Pass();
                }

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
        [InlineData("KestrelDesktop", "http://localhost:20207")]
        [InlineData("KestrelDesktopWithRuntimes", "http://localhost:20208")]
        public async Task DesktopApp_WithKestrel_WorksWhenRun(string project, string url)
        {
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
