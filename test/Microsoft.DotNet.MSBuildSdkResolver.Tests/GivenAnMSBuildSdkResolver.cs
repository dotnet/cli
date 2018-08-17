// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.Build.Framework;
using Microsoft.DotNet.MSBuildSdkResolver;
using Microsoft.DotNet.Tools.Test.Utilities;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;
using System;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Microsoft.DotNet.Cli.Utils.Tests
{
    public class GivenAnMSBuildSdkResolver : TestBase
    {
        private ITestOutputHelper _logger;

        public GivenAnMSBuildSdkResolver(ITestOutputHelper logger)
        {
            _logger = logger;
        }

        [Fact]
        public void ItHasCorrectNameAndPriority()
        {
            var resolver = new DotNetMSBuildSdkResolver();

            Assert.Equal(5000, resolver.Priority);
            Assert.Equal("Microsoft.DotNet.MSBuildSdkResolver", resolver.Name);
        }

        [Fact]
        public void ItFindsTheVersionSpecifiedInGlobalJson()
        {
            var environment = new TestEnvironment();
            environment.CreateSdkDirectory(ProgramFiles.X64, "Some.Test.Sdk", "99.99.97");
            var expected = environment.CreateSdkDirectory(ProgramFiles.X64, "Some.Test.Sdk", "99.99.98");
            environment.CreateSdkDirectory(ProgramFiles.X64, "Some.Test.Sdk", "99.99.99");
            environment.CreateGlobalJson(environment.TestDirectory, "99.99.98");
            environment.CreateMuxerAndAddToPath(ProgramFiles.X64);

            var resolver = environment.CreateResolver();
            var result = (MockResult)resolver.Resolve(
                new SdkReference("Some.Test.Sdk", null, null),
                new MockContext { ProjectFileDirectory = environment.TestDirectory },
                new MockFactory());

            result.Success.Should().BeTrue();
            result.Path.Should().Be(expected.FullName);
            result.Version.Should().Be("99.99.98");
            result.Warnings.Should().BeNullOrEmpty();
            result.Errors.Should().BeNullOrEmpty();
        }

        [Fact]
        public void ItReturnsNullIfTheVersionFoundDoesNotSatisfyTheMinVersion()
        {
            var environment = new TestEnvironment();
            environment.CreateSdkDirectory(ProgramFiles.X64, "Some.Test.Sdk", "99.99.99");
            environment.CreateMuxerAndAddToPath(ProgramFiles.X64);

            var resolver = environment.CreateResolver();
            var result = (MockResult)resolver.Resolve(
                new SdkReference("Some.Test.Sdk", null, "999.99.99"),
                new MockContext { ProjectFileDirectory = environment.TestDirectory },
                new MockFactory());

            result.Success.Should().BeFalse();
            result.Path.Should().BeNull();
            result.Version.Should().BeNull();
            result.Warnings.Should().BeNullOrEmpty();
            result.Errors.Should().Contain("Version 99.99.99 of the .NET Core SDK is smaller than the minimum version 999.99.99"
                + " requested. Check that a recent enough .NET Core SDK is installed, increase the minimum version"
                + " specified in the project, or increase the version specified in global.json.");
        }

        [Fact]
        public void ItReturnsNullWhenTheSDKRequiresAHigherVersionOfMSBuildThanAnyOneAvailable()
        {
            var environment = new TestEnvironment();
            var expected =
                environment.CreateSdkDirectory(ProgramFiles.X64, "Some.Test.Sdk", "99.99.99", new Version(2, 0));
            environment.CreateMuxerAndAddToPath(ProgramFiles.X64);

            var resolver = environment.CreateResolver();
            var result = (MockResult)resolver.Resolve(
                new SdkReference("Some.Test.Sdk", null, "99.99.99"),
                new MockContext
                {
                    MSBuildVersion = new Version(1, 0),
                    ProjectFileDirectory = environment.TestDirectory
                },
                new MockFactory());

            result.Success.Should().BeFalse();
            result.Path.Should().BeNull();
            result.Version.Should().BeNull();
            result.Warnings.Should().BeNullOrEmpty();
            result.Errors.Should().Contain("Version 99.99.99 of the .NET Core SDK requires at least version 2.0 of MSBuild."
                + " The current available version of MSBuild is 1.0. Change the .NET Core SDK specified in global.json to an older"
                + " version that requires the MSBuild version currently available.");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ItReturnsHighestSdkAvailableThatIsCompatibleWithMSBuild(bool disallowPreviews)
        {
            var environment = new TestEnvironment(identifier: disallowPreviews.ToString())
            {
                DisallowPrereleaseByDefault = disallowPreviews
            };

            var compatibleRtm = environment.CreateSdkDirectory(ProgramFiles.X64, "Some.Test.Sdk", "98.98.98", new Version(19, 0, 0, 0));
            var compatiblePreview = environment.CreateSdkDirectory(ProgramFiles.X64, "Some.Test.Sdk", "99.99.99-preview", new Version(20, 0, 0, 0));
            var incompatible = environment.CreateSdkDirectory(ProgramFiles.X64, "Some.Test.Sdk", "100.100.100", new Version(21, 0, 0, 0));

            environment.CreateMuxerAndAddToPath(ProgramFiles.X64);

            var resolver = environment.CreateResolver();
            var result = (MockResult)resolver.Resolve(
                new SdkReference("Some.Test.Sdk", null, null),
                new MockContext
                {
                    MSBuildVersion = new Version(20, 0, 0, 0),
                    ProjectFileDirectory = environment.TestDirectory,
                },
                new MockFactory());

            result.Success.Should().BeTrue();
            result.Path.Should().Be((disallowPreviews ? compatibleRtm : compatiblePreview).FullName);
            result.Version.Should().Be(disallowPreviews ? "98.98.98" : "99.99.99-preview");
            result.Warnings.Should().BeNullOrEmpty();
            result.Errors.Should().BeNullOrEmpty();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ItDoesNotReturnHighestSdkAvailableThatIsCompatibleWithMSBuildWhenVersionInGlobalJsonCannotBeFound(bool disallowPreviews)
        {
            var environment = new TestEnvironment(callingMethod: "ItDoesNotReturnHighest___", identifier: disallowPreviews.ToString())
            {
                DisallowPrereleaseByDefault = disallowPreviews
            };

            var compatibleRtm = environment.CreateSdkDirectory(ProgramFiles.X64, "Some.Test.Sdk", "98.98.98", new Version(19, 0, 0, 0));
            var compatiblePreview = environment.CreateSdkDirectory(ProgramFiles.X64, "Some.Test.Sdk", "99.99.99-preview", new Version(20, 0, 0, 0));
            var incompatible = environment.CreateSdkDirectory(ProgramFiles.X64, "Some.Test.Sdk", "100.100.100", new Version(21, 0, 0, 0));

            environment.CreateMuxerAndAddToPath(ProgramFiles.X64);
            environment.CreateGlobalJson(environment.TestDirectory, "1.2.3");

            var resolver = environment.CreateResolver();
            var result = (MockResult)resolver.Resolve(
                new SdkReference("Some.Test.Sdk", null, null),
                new MockContext
                {
                    MSBuildVersion = new Version(20, 0, 0, 0),
                    ProjectFileDirectory = environment.TestDirectory,
                },
                new MockFactory());

            result.Success.Should().BeFalse();
            result.Path.Should().BeNull();
            result.Version.Should().BeNull();;
            result.Warnings.Should().BeNullOrEmpty();
            result.Errors.Should().NotBeEmpty();
        }

        [Fact]
        public void ItReturnsNullWhenTheDefaultVSRequiredSDKVersionIsHigherThanTheSDKVersionAvailable()
        {
            var environment = new TestEnvironment();
            var expected =
                environment.CreateSdkDirectory(ProgramFiles.X64, "Some.Test.Sdk", "1.0.1");
            environment.CreateMuxerAndAddToPath(ProgramFiles.X64);

            var resolver = environment.CreateResolver();
            var result = (MockResult)resolver.Resolve(
                new SdkReference("Some.Test.Sdk", null, "1.0.0"),
                new MockContext { ProjectFileDirectory = environment.TestDirectory },
                new MockFactory());

            result.Success.Should().BeFalse();
            result.Path.Should().BeNull();
            result.Version.Should().BeNull();
            result.Warnings.Should().BeNullOrEmpty();
            result.Errors.Should().Contain($"Version 1.0.1 of the .NET Core SDK is smaller than the minimum version"
                            + " 1.0.4 required by Visual Studio. Check that a recent enough"
                            + " .NET Core SDK is installed or increase the version specified in global.json.");
        }

        [Fact]
        public void ItReturnsNullWhenTheTheVSRequiredSDKVersionIsHigherThanTheSDKVersionAvailable()
        {
            var environment = new TestEnvironment();
            var expected =
                environment.CreateSdkDirectory(ProgramFiles.X64, "Some.Test.Sdk", "1.0.1");
            environment.CreateMuxerAndAddToPath(ProgramFiles.X64);
            environment.CreateMinimumVSDefinedSDKVersionFile("2.0.0");

            var resolver = environment.CreateResolver();
            var result = (MockResult)resolver.Resolve(
                new SdkReference("Some.Test.Sdk", null, "1.0.0"),
                new MockContext { ProjectFileDirectory = environment.TestDirectory },
                new MockFactory());

            result.Success.Should().BeFalse();
            result.Path.Should().BeNull();
            result.Version.Should().BeNull();
            result.Warnings.Should().BeNullOrEmpty();
            result.Errors.Should().Contain($"Version 1.0.1 of the .NET Core SDK is smaller than the minimum version"
                            + " 2.0.0 required by Visual Studio. Check that a recent enough"
                            + " .NET Core SDK is installed or increase the version specified in global.json.");
        }

        [Fact]
        public void ItReturnsTheVersionIfItIsEqualToTheMinVersionAndTheVSDefinedMinVersion()
        {
            var environment = new TestEnvironment();
            var expected = environment.CreateSdkDirectory(ProgramFiles.X64, "Some.Test.Sdk", "99.99.99");
            environment.CreateMuxerAndAddToPath(ProgramFiles.X64);
            environment.CreateMinimumVSDefinedSDKVersionFile("99.99.99");

            var resolver = environment.CreateResolver();
            var result = (MockResult)resolver.Resolve(
                new SdkReference("Some.Test.Sdk", null, "99.99.99"),
                new MockContext { ProjectFileDirectory = environment.TestDirectory },
                new MockFactory());

            result.Success.Should().BeTrue();
            result.Path.Should().Be(expected.FullName);
            result.Version.Should().Be("99.99.99");
            result.Warnings.Should().BeNullOrEmpty();
            result.Errors.Should().BeNullOrEmpty();
        }

        [Fact]
        public void ItReturnsTheVersionIfItIsHigherThanTheMinVersionAndTheVSDefinedMinVersion()
        {
            var environment = new TestEnvironment();
            var expected = environment.CreateSdkDirectory(ProgramFiles.X64, "Some.Test.Sdk", "999.99.99");
            environment.CreateMuxerAndAddToPath(ProgramFiles.X64);
            environment.CreateMinimumVSDefinedSDKVersionFile("999.99.98");

            var resolver = environment.CreateResolver();
            var result = (MockResult)resolver.Resolve(
                new SdkReference("Some.Test.Sdk", null, "99.99.99"),
                new MockContext { ProjectFileDirectory = environment.TestDirectory },
                new MockFactory());

            result.Success.Should().BeTrue();
            result.Path.Should().Be(expected.FullName);
            result.Version.Should().Be("999.99.99");
            result.Warnings.Should().BeNullOrEmpty();
            result.Errors.Should().BeNullOrEmpty();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ItDisallowsPreviewsBasedOnDefault(bool disallowPreviewsByDefault)
        {
            var environment = new TestEnvironment(identifier: disallowPreviewsByDefault.ToString());
            var rtm = environment.CreateSdkDirectory(ProgramFiles.X64, "Some.Test.Sdk", "10.0.0");
            var preview = environment.CreateSdkDirectory(ProgramFiles.X64, "Some.Test.Sdk", "11.0.0-preview1");
            var expected = disallowPreviewsByDefault ? rtm : preview;

            environment.CreateMuxerAndAddToPath(ProgramFiles.X64);
            environment.DisallowPrereleaseByDefault = disallowPreviewsByDefault;

            var resolver = environment.CreateResolver();
            var result = (MockResult)resolver.Resolve(
                new SdkReference("Some.Test.Sdk", null, null),
                new MockContext { ProjectFileDirectory = environment.TestDirectory },
                new MockFactory());

            result.Success.Should().BeTrue();
            result.Path.Should().Be(expected.FullName);
            result.Version.Should().Be(disallowPreviewsByDefault ? "10.0.0" : "11.0.0-preview1");
            result.Warnings.Should().BeNullOrEmpty();
            result.Errors.Should().BeNullOrEmpty();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ItDisallowsPreviewsBasedOnFile(bool disallowPreviews)
        {
            var environment = new TestEnvironment(identifier: disallowPreviews.ToString());
            var rtm = environment.CreateSdkDirectory(ProgramFiles.X64, "Some.Test.Sdk", "10.0.0");
            var preview = environment.CreateSdkDirectory(ProgramFiles.X64, "Some.Test.Sdk", "11.0.0-preview1");
            var expected = disallowPreviews ? rtm : preview;

            environment.CreateMuxerAndAddToPath(ProgramFiles.X64);
            environment.DisallowPrereleaseByDefault = !disallowPreviews;
            environment.CreateVSSettingsFile(disallowPreviews);

            var resolver = environment.CreateResolver();
            var result = (MockResult)resolver.Resolve(
                new SdkReference("Some.Test.Sdk", null, null),
                new MockContext { ProjectFileDirectory = environment.TestDirectory },
                new MockFactory());

            result.Success.Should().BeTrue();
            result.Path.Should().Be(expected.FullName);
            result.Version.Should().Be(disallowPreviews ? "10.0.0" : "11.0.0-preview1");
            result.Warnings.Should().BeNullOrEmpty();
            result.Errors.Should().BeNullOrEmpty();
        }

        [Fact]
        public void ItObservesChangesToVSSettingsFile()
        {
            var environment = new TestEnvironment();
            var rtm = environment.CreateSdkDirectory(ProgramFiles.X64, "Some.Test.Sdk", "10.0.0");
            var preview = environment.CreateSdkDirectory(ProgramFiles.X64, "Some.Test.Sdk", "11.0.0-preview1");

            environment.CreateMuxerAndAddToPath(ProgramFiles.X64);
            environment.CreateVSSettingsFile(disallowPreviews: true);
            var resolver = environment.CreateResolver();

            void Check(bool disallowPreviews, string message)
            {
                // check twice because file-up-to-date is a separate code path
                for (int i = 0; i < 2; i++)
                {

                    var result = (MockResult)resolver.Resolve(
                        new SdkReference("Some.Test.Sdk", null, null),
                        new MockContext { ProjectFileDirectory = environment.TestDirectory },
                        new MockFactory());

                    string m = $"{message} ({i})";
                    var expected = disallowPreviews ? rtm : preview;
                    result.Success.Should().BeTrue(m);
                    result.Path.Should().Be(expected.FullName, m);
                    result.Version.Should().Be(disallowPreviews ? "10.0.0" : "11.0.0-preview1", m);
                    result.Warnings.Should().BeNullOrEmpty(m);
                    result.Errors.Should().BeNullOrEmpty(m);
                }
            }

            environment.DeleteVSSettingsFile();
            Check(disallowPreviews: false, message: "default with no file");

            environment.CreateVSSettingsFile(disallowPreviews: true);
            Check(disallowPreviews: true, message: "file changed to disallow previews");

            environment.CreateVSSettingsFile(disallowPreviews: false);
            Check(disallowPreviews: false,  message: "file changed to not disallow previews");

            environment.CreateVSSettingsFile(disallowPreviews: true);
            Check(disallowPreviews: true, message: "file changed back to disallow previews");

            environment.DeleteVSSettingsFile();
            Check(disallowPreviews: false, message: "file deleted to return to default");
        }

        [Fact]
        public void ItAllowsPreviewWhenGlobalJsonHasPreviewIrrespectiveOfSetting()
        {
            var environment = new TestEnvironment();
            var rtm = environment.CreateSdkDirectory(ProgramFiles.X64, "Some.Test.Sdk", "10.0.0");
            var preview = environment.CreateSdkDirectory(ProgramFiles.X64, "Some.Test.Sdk", "11.0.0-preview1");
 
            environment.CreateMuxerAndAddToPath(ProgramFiles.X64);
            environment.DisallowPrereleaseByDefault = true;
            environment.CreateGlobalJson(environment.TestDirectory, "11.0.0-preview1");

            var resolver = environment.CreateResolver();
            var result = (MockResult)resolver.Resolve(
                new SdkReference("Some.Test.Sdk", null, null),
                new MockContext { ProjectFileDirectory = environment.TestDirectory },
                new MockFactory());

            result.Success.Should().BeTrue();
            result.Path.Should().Be(preview.FullName);
            result.Version.Should().Be("11.0.0-preview1");
            result.Warnings.Should().BeNullOrEmpty();
            result.Errors.Should().BeNullOrEmpty();
        }

        [Fact]
        public void ItRespectsAmbientVSSettings()
        {
            // When run in test explorer in VS, this will actually locate the settings for the current VS instance
            // based on location of testhost executable. This gives us some coverage threw that path but we cannot
            // fix our expectations since the behavior will vary (by design) based on the current VS instance's settings.
            var vsSettings = VSSettings.Ambient;

            var environment = new TestEnvironment();
            var rtm = environment.CreateSdkDirectory(ProgramFiles.X64, "Some.Test.Sdk", "10.0.0");
            var preview = environment.CreateSdkDirectory(ProgramFiles.X64, "Some.Test.Sdk", "11.0.0-preview1");
            var expected = vsSettings.DisallowPrerelease() ? rtm : preview;

            environment.CreateMuxerAndAddToPath(ProgramFiles.X64);

            var resolver = environment.CreateResolver(useAmbientSettings: true);
            var result = (MockResult)resolver.Resolve(
                new SdkReference("Some.Test.Sdk", null, null),
                new MockContext { ProjectFileDirectory = environment.TestDirectory },
                new MockFactory());

            result.Success.Should().BeTrue();
            result.Path.Should().Be(expected.FullName);
            result.Version.Should().Be(vsSettings.DisallowPrerelease() ? "10.0.0" : "11.0.0-preview1");
            result.Warnings.Should().BeNullOrEmpty();
            result.Errors.Should().BeNullOrEmpty();
        }

        private enum ProgramFiles
        {
            X64,
            X86,
            Default,
        }

        private sealed class TestEnvironment : SdkResolverContext
        {
            public string Muxer => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet";

            public string PathEnvironmentVariable { get; set; }

            public DirectoryInfo TestDirectory { get; }
            public FileInfo VSSettingsFile { get; set; }
            public bool DisallowPrereleaseByDefault { get; set; }

            public TestEnvironment(string identifier = "", [CallerMemberName] string callingMethod = "")
            {
                TestDirectory = TestAssets.CreateTestDirectory(
                    "temp",
                    identifier: identifier,
                    callingMethod: callingMethod);

                DeleteMinimumVSDefinedSDKVersionFile();

                PathEnvironmentVariable = string.Empty;
            }

            public SdkResolver CreateResolver(bool useAmbientSettings = false)
                => new DotNetMSBuildSdkResolver(
                    GetEnvironmentVariable, 
                    useAmbientSettings
                        ? VSSettings.Ambient
                        : new VSSettings(VSSettingsFile?.FullName, DisallowPrereleaseByDefault));

            public DirectoryInfo GetSdkDirectory(ProgramFiles programFiles, string sdkName, string sdkVersion)
                => TestDirectory.GetDirectory(
                    GetProgramFilesDirectory(programFiles).FullName,
                    "dotnet",
                    "sdk",
                    sdkVersion,
                    "Sdks",
                    sdkName,
                    "Sdk");

            public DirectoryInfo GetProgramFilesDirectory(ProgramFiles programFiles)
                => TestDirectory.GetDirectory($"ProgramFiles{programFiles}");
            
            public DirectoryInfo CreateSdkDirectory(
                ProgramFiles programFiles,
                string sdkName,
                string sdkVersion,
                Version minimumMSBuildVersion = null)
            {
                var dir = GetSdkDirectory(programFiles, sdkName, sdkVersion);
                dir.Create();

                if (minimumMSBuildVersion != null)
                {
                    CreateMSBuildRequiredVersionFile(programFiles, sdkVersion, minimumMSBuildVersion);
                }

                return dir;
            }

            public void CreateMuxerAndAddToPath(ProgramFiles programFiles)
            {
                var muxerDirectory =
                    TestDirectory.GetDirectory(GetProgramFilesDirectory(programFiles).FullName, "dotnet");

                new FileInfo(Path.Combine(muxerDirectory.FullName, Muxer)).Create();

                PathEnvironmentVariable = $"{muxerDirectory}{Path.PathSeparator}{PathEnvironmentVariable}";
            }

            private void CreateMSBuildRequiredVersionFile(
                ProgramFiles programFiles,
                string sdkVersion,
                Version minimumMSBuildVersion)
            {
                if (minimumMSBuildVersion == null)
                {
                    minimumMSBuildVersion = new Version(1, 0);
                }

                var cliDirectory = TestDirectory.GetDirectory(
                    GetProgramFilesDirectory(programFiles).FullName,
                    "dotnet",
                    "sdk",
                    sdkVersion);

                File.WriteAllText(
                    Path.Combine(cliDirectory.FullName, "minimumMSBuildVersion"),
                    minimumMSBuildVersion.ToString());
            }

            public void CreateGlobalJson(DirectoryInfo directory, string version)
                => File.WriteAllText(directory.GetFile("global.json").FullName, 
                    $@"{{ ""sdk"": {{ ""version"":  ""{version}"" }} }}");

            public string GetEnvironmentVariable(string variable)
            {
                switch (variable)
                {
                    case "PATH":
                        return PathEnvironmentVariable;
                    default:
                        return null;
                }
            }

            public void CreateMinimumVSDefinedSDKVersionFile(string version)
            {
                File.WriteAllText(GetMinimumVSDefinedSDKVersionFilePath(), version);
            }

            private void DeleteMinimumVSDefinedSDKVersionFile()
            {                
                File.Delete(GetMinimumVSDefinedSDKVersionFilePath());
            }

            private string GetMinimumVSDefinedSDKVersionFilePath()
            {
                string baseDirectory = AppContext.BaseDirectory;
                return Path.Combine(baseDirectory, "minimumVSDefinedSDKVersion");
            }

            public void CreateVSSettingsFile(bool disallowPreviews)
            {
                VSSettingsFile = TestDirectory.GetFile("sdk.txt");

                // Guard against tests writing too fast for the up-to-date check
                // It happens more often on Unix due to https://github.com/dotnet/corefx/issues/12403
                var lastWriteTimeUtc = VSSettingsFile.Exists ? VSSettingsFile.LastWriteTimeUtc : DateTime.MinValue;
                for (int sleep = 10; sleep < 3000; sleep *= 2)
                {
                    File.WriteAllText(VSSettingsFile.FullName, $"UsePreviews={!disallowPreviews}");
                    VSSettingsFile.Refresh();

                    if (VSSettingsFile.LastWriteTimeUtc > lastWriteTimeUtc)
                    {
                        return;
                    }

                    System.Threading.Thread.Sleep(sleep);
                }

                throw new InvalidOperationException("LastWriteTime is not changing.");
            }

            public void DeleteVSSettingsFile()
            {
                VSSettingsFile.Delete();
            }
        }

        private sealed class MockContext : SdkResolverContext
        {
            public new string ProjectFilePath { get => base.ProjectFilePath; set => base.ProjectFilePath = value; }
            public new string SolutionFilePath { get => base.SolutionFilePath; set => base.SolutionFilePath = value; }
            public new Version MSBuildVersion { get => base.MSBuildVersion; set => base.MSBuildVersion = value; }

            public DirectoryInfo ProjectFileDirectory
            {
                get => new DirectoryInfo(Path.GetDirectoryName(ProjectFilePath));
                set => ProjectFilePath = value.GetFile("test.csproj").FullName;
            }

            public MockContext()
            {
                MSBuildVersion = new Version(15, 3, 0);
            }
        }

        private sealed class MockFactory : SdkResultFactory
        {
            public override SdkResult IndicateFailure(IEnumerable<string> errors, IEnumerable<string> warnings = null)
                => new MockResult(success: false, path: null, version: null, warnings: warnings, errors: errors);

            public override SdkResult IndicateSuccess(string path, string version, IEnumerable<string> warnings = null)
                => new MockResult(success: true, path: path, version: version, warnings: warnings);
        }

        private sealed class MockResult : SdkResult
        {
            public MockResult(bool success, string path, string version, IEnumerable<string> warnings = null,
                IEnumerable<string> errors = null)
            {
                Success = success;
                Path = path;
                Version = version;
                Warnings = warnings;
                Errors = errors;
            }

            public override bool Success { get; protected set; }
            public override string Version { get; protected set; }
            public override string Path { get; protected set; }
            public IEnumerable<string> Errors { get; }
            public IEnumerable<string> Warnings { get; }
        }
    }
}
