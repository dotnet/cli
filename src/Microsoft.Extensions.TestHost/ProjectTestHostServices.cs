// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Dnx.Runtime.Common.DependencyInjection;
using Microsoft.Dnx.TestHost.TestAdapter;
using Microsoft.Dnx.Testing.Abstractions;
using Microsoft.Extensions.Compilation;
using Microsoft.Extensions.Logging;

namespace Microsoft.Dnx.TestHost
{
    internal class ProjectTestHostServices: TestHostServices
    {
        public ProjectTestHostServices(
            Project project,
            ReportingChannel channel)
        {
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(new TestHostLoggerProvider(channel));
            var export = CompilationServices.Default.LibraryExporter.GetExport(project.Name);

            var projectReference = export.MetadataReferences
                .OfType<IMetadataProjectReference>()
                .Where(r => r.Name == project.Name)
                .FirstOrDefault();

            SourceInformationProvider =
                new SourceInformationProvider(projectReference, loggerFactory.CreateLogger<SourceInformationProvider>());

            TestDiscoverySink = new TestDiscoverySink(channel);
            TestExecutionSink = new TestExecutionSink(channel);
            LoggerFactory = loggerFactory;
        }

        public override ITestDiscoverySink TestDiscoverySink { get; }

        public override ITestExecutionSink TestExecutionSink { get; }

        public override ISourceInformationProvider SourceInformationProvider { get; }

        public override ILoggerFactory LoggerFactory { get; }
    }
}