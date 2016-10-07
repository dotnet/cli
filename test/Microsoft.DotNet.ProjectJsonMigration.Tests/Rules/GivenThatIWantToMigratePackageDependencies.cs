// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Build.Construction;
using Microsoft.DotNet.Tools.Test.Utilities;
using System.Linq;
using Xunit;
using FluentAssertions;
using Microsoft.DotNet.ProjectJsonMigration.Rules;
using System;

namespace Microsoft.DotNet.ProjectJsonMigration.Tests
{
    public class GivenThatIWantToMigratePackageDependencies : TestBase
    {
        [Fact]
        public void It_migrates_basic_PackageReference()
        {
            var mockProj = RunPackageDependenciesRuleOnPj(@"                
                {
                    ""dependencies"": {
                        ""APackage"" : ""1.0.0-preview"",
                        ""BPackage"" : ""1.0.0""
                    }
                }");
            
            EmitsPackageReferences(mockProj, Tuple.Create("APackage", "1.0.0-preview", ""), Tuple.Create("BPackage", "1.0.0", ""));            
        }

        [Fact]
        public void It_migrates_Tools()
        {
            var mockProj = RunPackageDependenciesRuleOnPj(@"                
                {
                    ""tools"": {
                        ""APackage"" : ""1.0.0-preview"",
                        ""BPackage"" : ""1.0.0""
                    }
                }");
            
            EmitsToolReferences(mockProj, Tuple.Create("APackage", "1.0.0-preview"), Tuple.Create("BPackage", "1.0.0"));            
        }

        [Fact]
        public void It_migrates_imports_per_framework()
        {
            var importPropertyName = "PackageTargetFallback";

            var mockProj = RunPackageDependenciesRuleOnPj(@"                
                {
                    ""frameworks"": {
                        ""netcoreapp1.0"" : {
                            ""imports"": [""netstandard1.3"", ""net451""]
                        },
                        ""netstandard1.3"" : {
                            ""imports"": [""net451""]
                        },
                        ""net451"" : {
                            ""imports"": ""netstandard1.3""
                        }
                    }                    
                }");

            var imports = mockProj.Properties.Where(p => p.Name == importPropertyName);
            imports.Should().HaveCount(3);

            var netcoreappImport = imports.First(p => p.Condition.Contains("netcoreapp1.0"));
            var netstandardImport = imports.First(p => p.Condition.Contains("netstandard1.3"));
            var net451Import = imports.First(p => p.Condition.Contains("net451"));

            netcoreappImport.Should().NotBe(netstandardImport);

            netcoreappImport.Condition.Should().Be(" '$(TargetFramework)' == 'netcoreapp1.0' ");
            netstandardImport.Condition.Should().Be(" '$(TargetFramework)' == 'netstandard1.3' ");
            net451Import.Condition.Should().Be(" '$(TargetFramework)' == 'net451' ");

            netcoreappImport.Value.Split(';').Should().BeEquivalentTo($"$({importPropertyName})", "netstandard1.3", "net451");
            netstandardImport.Value.Split(';').Should().BeEquivalentTo($"$({importPropertyName})", "net451");
            net451Import.Value.Split(';').Should().BeEquivalentTo($"$({importPropertyName})", "netstandard1.3");
        }

        private void EmitsPackageReferences(ProjectRootElement mockProj, params Tuple<string, string, string>[] packageSpecs)
        {
            foreach (var packageSpec in packageSpecs)
            {
                var packageName = packageSpec.Item1;
                var packageVersion = packageSpec.Item2;
                var packageTFM = packageSpec.Item3;

                var items = mockProj.Items
                    .Where(i => i.ItemType == "PackageReference")
                    .Where(i => string.IsNullOrEmpty(packageTFM) || i.ConditionChain().Any(c => c.Contains(packageTFM)))
                    .Where(i => i.Include == packageName)
                    .Where(i => i.GetMetadataWithName("Version").Value == packageVersion);

                items.Should().HaveCount(1);
            }
        }

        private void EmitsToolReferences(ProjectRootElement mockProj, params Tuple<string, string>[] toolSpecs)
        {
            foreach (var toolSpec in toolSpecs)
            {
                var packageName = toolSpec.Item1;
                var packageVersion = toolSpec.Item2;

                var items = mockProj.Items
                    .Where(i => i.ItemType == "DotNetCliToolReference")
                    .Where(i => i.Include == packageName)
                    .Where(i => i.GetMetadataWithName("Version").Value == packageVersion);

                items.Should().HaveCount(1);
            }
        }

        private ProjectRootElement RunPackageDependenciesRuleOnPj(string s, string testDirectory = null)
        {
            testDirectory = testDirectory ?? Temp.CreateDirectory().Path;
            return TemporaryProjectFileRuleRunner.RunRules(new IMigrationRule[]
            {
                new MigratePackageDependenciesAndToolsRule()
            }, s, testDirectory);
        }
    }
}