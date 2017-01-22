using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.DotNet.TestFramework;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;
using FluentAssertions;
using Microsoft.DotNet.Cli.Utils;

namespace Microsoft.DotNet.New.Tests
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
        [InlineData("F#", "mvc", true)]
        public void TemplateRestoresAndBuildsWithoutWarnings(
            string language,
            string projectType,
            bool useNuGetConfigForAspNet)
        {
            string rootPath = TestAssetsManager.CreateTestDirectory(identifier: $"{language}_{projectType}").Path;

            new TestCommand("dotnet") { WorkingDirectory = rootPath }
                .Execute($"new {projectType} -lang {language} -o {rootPath}")
                .Should().Pass();

            if (useNuGetConfigForAspNet)
            {
                File.Copy("NuGet.tempaspnetpatch.config", Path.Combine(rootPath, "NuGet.Config"));
            }

            // note (scp 2017-01-17): we keep going back & forth whether to have global.json in the templates.
            //  at this point we do not, but when we do, this part of the test will become valid again.
            //string globalJsonPath = Path.Combine(rootPath, "global.json");
            //Assert.True(File.Exists(globalJsonPath));
            //Assert.Contains(Product.Version, File.ReadAllText(globalJsonPath));

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
