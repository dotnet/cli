// Copyright (c) .NET Foundation and contributors. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.TestFramework;
using Microsoft.DotNet.Tools.Test.Utilities;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;
using FluentAssertions;
using Microsoft.DotNet.PlatformAbstractions;

namespace Microsoft.DotNet.Tests
{
    public class GivenThatIWantToManageMulticoreJIT : TestBase
    {
        ITestOutputHelper _output;
        private const string OptimizationProfileFileName = "dotnet";
        
        public GivenThatIWantToManageMulticoreJIT(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void WhenInvokedThenDotnetWritesOptimizationDataToTheProfileRoot()
        {
            var testDirectory = TestAssets.CreateTestDirectory();
            var testStartTime = GetTruncatedDateTime();
                        
            new TestCommand("dotnet")
                .WithUserProfileRoot(testDirectory)
                .ExecuteWithCapturedOutput("--help");

            var optimizationProfileFile = GetOptimizationProfileFile(testDirectory);

           optimizationProfileFile
                .Should().Exist("Because dotnet CLI creates it after each run")
                     .And.HaveLastWriteTimeUtc()
                         .Which.Should().BeOnOrAfter(testStartTime, "Because dotnet CLI was executed after that time");
        }

        [Fact]
        public void WhenInvokedWithMulticoreJitDisabledThenDotnetDoesNotWriteOptimizationDataToTheProfileRoot()
        {
            var testDirectory = TestAssets.CreateTestDirectory();
            var testStartTime = GetTruncatedDateTime();
                        
            new TestCommand("dotnet")
                .WithUserProfileRoot(testDirectory)
                .WithEnvironmentVariable("DOTNET_DISABLE_MULTICOREJIT", "1")
                .ExecuteWithCapturedOutput("--help");

            var optimizationProfileFile = GetOptimizationProfileFile(testDirectory);

            optimizationProfileFile
                .Should().NotExist("Because multicore JIT is disabled");
        }

        [Fact]
        public void WhenTheProfileRootIsUndefinedThenDotnetDoesNotCrash()
        {
            var testDirectory = TestAssets.CreateTestDirectory();
            var testStartTime = GetTruncatedDateTime();
            
            var optimizationProfileFile = GetOptimizationProfileFile(testDirectory);

            new TestCommand("dotnet")
                .WithUserProfileRoot("")
                .ExecuteWithCapturedOutput("--help")
                .Should().Pass();
        }

        [Fact]
        public void WhenCliRepoBuildsThenDotnetWritesOptimizationDataToTheDefaultProfileRoot()
        {
            var optimizationProfileFile = GetOptimizationProfileFile();

            optimizationProfileFile
                .Should().Exist("Because the dotnet building dotnet writes to the default root");
        }

        private static FileInfo GetOptimizationProfileFile(DirectoryInfo userHomeDirectory = null)
        {
            return new FileInfo(Path.Combine(
                GetUserProfileRoot(userHomeDirectory).FullName, 
                GetOptimizationRootPath(GetDotnetVersion()),
                OptimizationProfileFileName));
        }
        
        private static DirectoryInfo GetUserProfileRoot(DirectoryInfo overrideUserProfileRoot = null)
        {
            if (overrideUserProfileRoot != null)
            {
                return overrideUserProfileRoot;
            }

            var userProfileRootPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? Environment.GetEnvironmentVariable("LocalAppData")
                : Environment.GetEnvironmentVariable("HOME");
            
            return new DirectoryInfo(userProfileRootPath);
        }

        private static string GetOptimizationRootPath(string version)
        {
            var rid = RuntimeEnvironment.GetRuntimeIdentifier();
            
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? $@"Microsoft\dotnet\optimizationdata\{version}\{rid}" 
                : $@".dotnet/optimizationdata/{version}/{rid}";
        }

        private static string GetDotnetVersion()
        {
            return new TestCommand("dotnet")
                .ExecuteWithCapturedOutput("--version" )
                .StdOut
                .Trim();
        }
        
        private static DateTime GetTruncatedDateTime()
        {
            var dt = DateTime.UtcNow;
            
            return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, 0, dt.Kind);
        }
    }
}
