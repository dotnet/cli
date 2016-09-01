
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.ProjectModel.Compilation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Tools.Test.Utilities;
using NuGet.Frameworks;

namespace Microsoft.DotNet.ProjectModel.Tests
{
    public class GivenAProjectContext : TestBase
    {
        [Fact]
        public void It_caches_library_exporter_when_configuration_and_buildBasePath_keep_value()
        {
            var projectContext = GetProjectContext();

            var configurations = new string[] { "TestConfig", "TestConfig", "TestConfig2", "TestConfig2" };
            var buildBasePaths = new string[] { null, AppContext.BaseDirectory, null, AppContext.BaseDirectory };

            for(int i=0; i<configurations.Length; ++i)
            {
                var configuration = configurations[i];
                var buildBasePath = buildBasePaths[i];

                var exporter1 = projectContext.CreateExporter(configuration, buildBasePath);
                var exporter2 = projectContext.CreateExporter(configuration, buildBasePath);

                ReferenceEquals(exporter1, exporter2).Should().BeTrue();
            }
        }

        [Fact]
        public void It_does_not_cache_library_exporter_when_configuration_or_buildBasePath_changes()
        {
            var projectContext = GetProjectContext();

            var configurations = new string[] { "TestConfig", "TestConfig", "TestConfig2", "TestConfig2" };
            var buildBasePaths = new string[] { null, AppContext.BaseDirectory, null, AppContext.BaseDirectory };
            var previousExporters = new List<LibraryExporter>();

            for(int i=0; i<configurations.Length; ++i)
            {
                var configuration = configurations[i];
                var buildBasePath = buildBasePaths[i];
                var exporter = projectContext.CreateExporter(configuration, buildBasePath);

                foreach (var previousExporter in previousExporters)
                {
                    ReferenceEquals(exporter, previousExporter).Should().BeFalse();
                }

                previousExporters.Add(exporter);
            }
        }

        private ProjectContext GetProjectContext()
        {
            var testInstance = TestAssetsManager.CreateTestInstance("TestAppSimple")
                                                .WithLockFiles();

            return ProjectContext.Create(testInstance.TestRoot, FrameworkConstants.CommonFrameworks.NetCoreApp10);
        }
    }
}