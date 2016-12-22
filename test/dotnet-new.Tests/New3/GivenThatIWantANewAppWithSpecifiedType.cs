using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.DotNet.TestFramework;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;
using FluentAssertions;

namespace Microsoft.DotNet.New3.Tests
{
    public class GivenThatIWantANewAppWithSpecifiedType : TestBase
    {
        [Theory]
        [InlineData("C#", "console", false)]
        [InlineData("C#", "classlib", false)]
        [InlineData("C#", "mstest", false)]
        [InlineData("C#", "xunit", false)]
        [InlineData("C#", "web", true)]
        [InlineData("C#", "mvc", true)]
        [InlineData("C#", "webapi", true)]
        [InlineData("F#", "console", false)]
        [InlineData("F#", "classlib", false)]
        [InlineData("F#", "mstest", false)]
        [InlineData("F#", "xunit", false)]
        [InlineData("F#", "web", true)]
        public void TemplateRestoresAndBuildsWithoutWarnings(
            string language,
            string projectType,
            bool useNuGetConfigForAspNet)
        {
            string rootPath = TestAssetsManager.CreateTestDirectory(identifier: $"{language}_{projectType}").Path;

            new TestCommand("dotnet")
                .WithWorkingDirectory(rootPath)
                .Execute($"new3 {projectType} --lang {language}")
                .Should().Pass();

            if (useNuGetConfigForAspNet)
            {
                File.Copy("NuGet.tempaspnetpatch.config", Path.Combine(rootPath, "NuGet.Config"));
            }

            new TestCommand("dotnet")
                .WithWorkingDirectory(rootPath)
                .Execute($"restore")
                .Should().Pass();

            var buildResult = new TestCommand("dotnet")
                .WithWorkingDirectory(rootPath)
                .ExecuteWithCapturedOutput("build")
                .Should().Pass()
                .And.NotHaveStdErr();
        }
    }
}
