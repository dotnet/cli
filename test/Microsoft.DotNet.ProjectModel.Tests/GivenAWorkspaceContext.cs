// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using FluentAssertions;
using Microsoft.DotNet.TestFramework;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;

namespace Microsoft.DotNet.ProjectModel.Tests
{
    public class GivenAWorkspaceContext : TestBase
    {
        [Fact]
        public void GetProjectContextCollectionWorksForALocalProjectJson()
        {
            string projectJsonFileName = Project.FileName;

            // normally the test will be run in the test directory - which already has a project.json file.
            // But, if a local project.json file doesn't exist, copy one locally
            if (!File.Exists(projectJsonFileName))
            {
                string projectJsonPath = Path.Combine(TestAssetsManager.AssetsRoot, "TestAppSimple", projectJsonFileName);
                File.Copy(projectJsonPath, projectJsonFileName, true);
            }

            WorkspaceContext context = WorkspaceContext.Create(designTime: false);
            ProjectContextCollection contexts = context.GetProjectContextCollection(projectJsonFileName);

            contexts.Should().NotBeNull();
            contexts.Project.Should().NotBeNull();
            contexts.Project.Name.Should().Be(new DirectoryInfo(Directory.GetCurrentDirectory()).Name);
        }
    }
}
