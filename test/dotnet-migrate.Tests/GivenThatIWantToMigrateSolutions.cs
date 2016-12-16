// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Xunit;
using Microsoft.DotNet.TestFramework;
using Microsoft.DotNet.Tools.Test.Utilities;

namespace Microsoft.DotNet.Migration.Tests
{
    public class GivenThatIWantToMigrateSolutions : TestBase
    {
        [Fact]
        public void ItMigratesSln()
        {
            //var rootDirectory = TestAssetsManager.CreateTestInstance(
            //    "TestAppWithSlnAndMultipleProjects",
            //    callingMethod: "a").Path;

            //var testAppProjectDirectory = Path.Combine(rootDirectory, "TestApp");
            //var testLibProjectDirectory = Path.Combine(rootDirectory, "TestLibrary");
            //string slnPath = Path.Combine(testAppProjectDirectory, "TestApp.sln");
            
            //CleanBinObj(testAppProjectDirectory);
            //CleanBinObj(testLibProjectDirectory);

            //MigrateProject(slnPath);
            //Restore(testAppProjectDirectory, "TestApp.csproj");
            //BuildMSBuild(testAppProjectDirectory, "TestApp.sln", "Release");
        }
    }
}
