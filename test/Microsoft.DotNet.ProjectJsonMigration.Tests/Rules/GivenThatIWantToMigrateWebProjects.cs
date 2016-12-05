// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Build.Construction;
using Microsoft.DotNet.Tools.Test.Utilities;
using System.Linq;
using Xunit;
using FluentAssertions;
using Microsoft.DotNet.ProjectJsonMigration;
using Microsoft.DotNet.ProjectJsonMigration.Rules;
using System;
using System.IO;

namespace Microsoft.DotNet.ProjectJsonMigration.Tests
{
    public class GivenThatIWantToMigrateWebProjects : PackageDependenciesTestBase
    {
        [Fact]
        public void ItMigratesWebProjectsToHaveWebSdkInTheSdkAttribute()
        {
            var csprojFilePath = RunMigrateWebSdkRuleOnPj(@"
                {
                    ""buildOptions"": {
                        ""emitEntryPoint"": true
                    },
                    ""dependencies"": {
                        ""Microsoft.AspNetCore.Mvc"" : {
                            ""version"": ""1.0.0""
                        }
                    },
                    ""frameworks"": {
                        ""netcoreapp1.0"": {}
                    }
                }");

            File.ReadAllText(csprojFilePath).Should().Contain(@"Sdk=""Microsoft.NET.Sdk.Web""");
        }

        private string RunMigrateWebSdkRuleOnPj(string s, string testDirectory = null)
        {
            testDirectory = testDirectory ?? Temp.CreateDirectory().Path;
            var csprojFilePath = Path.Combine(testDirectory, "migrateWebSdkRule.csproj");
            var mockProj = TemporaryProjectFileRuleRunner.RunRules(new IMigrationRule[]
            {
                new MigrateWebSdkRule()
            }, s, testDirectory);

            mockProj.Save(csprojFilePath);

            return csprojFilePath;
        }
    }
}