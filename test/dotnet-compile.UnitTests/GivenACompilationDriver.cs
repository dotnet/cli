// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.Tools.Compiler;
using Moq;
using NuGet.Frameworks;
using Xunit;

namespace Microsoft.DotNet.Tools.Compiler.Tests
{
    public class GivenACompilationDriverController
    {
        private string _projectJson;
        private Mock<IManagedCompiler> _managedCompilerMock;
        private List<ProjectContext> _contexts;
        private CompilerCommandApp _args;

        public GivenACompilationDriverController()
        {
            _projectJson =
                Path.Combine(AppContext.BaseDirectory, "TestAssets", "TestProjects", "TestAppWithLibrary", "TestApp", "project.json");
            _managedCompilerMock = new Mock<IManagedCompiler>();
            _managedCompilerMock.Setup(c => c
                .Compile(It.IsAny<ProjectContext>(), It.IsAny<CompilerCommandApp>()))
                .Returns(true);

            _contexts = new List<ProjectContext>
            {
                ProjectContext.Create(_projectJson, NuGetFramework.Parse("netstandardapp1.5"))
            };

            _args = new CompilerCommandApp("dotnet compile", ".NET Compiler", "Compiler for the .NET Platform");
        }

        [Fact]
        public void It_compiles_all_project_contexts()
        {
            var compiledProjectContexts = new List<ProjectContext>();
            _managedCompilerMock.Setup(c => c
                .Compile(It.IsAny<ProjectContext>(), It.IsAny<CompilerCommandApp>()))
                .Callback<ProjectContext, CompilerCommandApp>((p, c) => compiledProjectContexts.Add(p))
                .Returns(true);

            var compilerController = new CompilationDriver(_managedCompilerMock.Object);

            compilerController.Compile(_contexts, _args);

            compiledProjectContexts.Should().BeEquivalentTo(_contexts);
        }
    }
}
