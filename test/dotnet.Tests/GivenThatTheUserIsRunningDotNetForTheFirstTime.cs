// Copyright (c) .NET Foundation and contributors. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.TestFramework;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;
using FluentAssertions;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Microsoft.DotNet.Tests
{
    public class GivenThatTheUserIsRunningDotNetForTheFirstTime : TestBase
    {
        private static CommandResult _firstDotnetNonVerbUseCommandResult;
        private static CommandResult _firstDotnetVerbUseCommandResult;
        private static DirectoryInfo _nugetCacheFolder;

        static GivenThatTheUserIsRunningDotNetForTheFirstTime()
        {
            var testDirectory = TestAssetsManager.CreateTestDirectory("Dotnet_first_time_experience_tests");
            var testNugetCache = Path.Combine(testDirectory.Path, "nuget_cache");

            var command = new DotnetCommand()
                .WithWorkingDirectory(testDirectory.Path);
            command.Environment["NUGET_PACKAGES"] = testNugetCache;
            command.Environment["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "";
            command.Environment["SkipInvalidConfigurations"] = "true";

            _firstDotnetNonVerbUseCommandResult = command.ExecuteWithCapturedOutput("--info");
            _firstDotnetVerbUseCommandResult = command.ExecuteWithCapturedOutput("new --debug:ephemeral-hive");

            _nugetCacheFolder = new DirectoryInfo(testNugetCache);
        }

        [Fact]
        public void UsingDotnetNonVerbForTheFirstTimeSucceeds()
        {
            _firstDotnetNonVerbUseCommandResult
                .Should()
                .Pass();
        }

        [Fact]
        public void UsingDotnetVerbForTheFirstTimeSucceeds()
        {
            _firstDotnetVerbUseCommandResult
                .Should()
                .Pass();
        }

        [Fact]
        public void UsingDotnetForTheFirstTimeWithNonVerbsDoesNotPrintEula()
        {
            const string firstTimeNonVerbUseMessage = @".NET Command Line Tools";

            _firstDotnetNonVerbUseCommandResult.StdOut
                .Should()
                .StartWith(firstTimeNonVerbUseMessage);
        }

        [Fact]
        public void ItShowsTheAppropriateMessageToTheUser()
        {
            string firstTimeUseWelcomeMessage = NormalizeLineEndings(@"Welcome to .NET Core!
---------------------
Learn more about .NET Core @ https://aka.ms/dotnet-docs. Use dotnet --help to see available commands or go to https://aka.ms/dotnet-cli-docs.

Telemetry
--------------
The .NET Core tools collect usage data in order to improve your experience. The data is anonymous and does not include command-line arguments. The data is collected by Microsoft and shared with the community.
You can opt out of telemetry by setting a DOTNET_CLI_TELEMETRY_OPTOUT environment variable to 1 using your favorite shell.
You can read more about .NET Core tools telemetry @ https://aka.ms/dotnet-cli-telemetry.

Configuring...
-------------------
A command is running to initially populate your local package cache, to improve restore speed and enable offline access. This command will take up to a minute to complete and will only happen once.");

            // normalizing line endings as git is occasionally replacing line endings in this file causing this test to fail
            NormalizeLineEndings(_firstDotnetVerbUseCommandResult.StdOut)
                .Should().Contain(firstTimeUseWelcomeMessage)
                     .And.NotContain("Restore completed in");
        }

        [Fact]
        public void ItCreatesASentinelFileUnderTheNuGetCacheFolder()
        {
            _nugetCacheFolder
                .Should()
                .HaveFile($"{GetDotnetVersion()}.dotnetSentinel");
        }

        [Fact]
        public void ItRestoresTheNuGetPackagesToTheNuGetCacheFolder()
        {
            List<string> expectedDirectories = new List<string>()
            {
                "libuv",
                "microsoft.aspnetcore",
                "microsoft.aspnetcore.antiforgery",
                "microsoft.aspnetcore.authentication",
                "microsoft.aspnetcore.authentication.cookies",
                "microsoft.aspnetcore.authorization",
                "microsoft.aspnetcore.cors",
                "microsoft.aspnetcore.cryptography.internal",
                "microsoft.aspnetcore.cryptography.keyderivation",
                "microsoft.aspnetcore.dataprotection",
                "microsoft.aspnetcore.dataprotection.abstractions",
                "microsoft.aspnetcore.diagnostics",
                "microsoft.aspnetcore.diagnostics.abstractions",
                "microsoft.aspnetcore.diagnostics.entityframeworkcore",
                "microsoft.aspnetcore.hosting",
                "microsoft.aspnetcore.hosting.abstractions",
                "microsoft.aspnetcore.hosting.server.abstractions",
                "microsoft.aspnetcore.html.abstractions",
                "microsoft.aspnetcore.http",
                "microsoft.aspnetcore.http.abstractions",
                "microsoft.aspnetcore.http.extensions",
                "microsoft.aspnetcore.http.features",
                "microsoft.aspnetcore.httpoverrides",
                "microsoft.aspnetcore.identity",
                "microsoft.aspnetcore.identity.entityframeworkcore",
                "microsoft.aspnetcore.jsonpatch",
                "microsoft.aspnetcore.localization",
                "microsoft.aspnetcore.mvc",
                "microsoft.aspnetcore.mvc.abstractions",
                "microsoft.aspnetcore.mvc.apiexplorer",
                "microsoft.aspnetcore.mvc.core",
                "microsoft.aspnetcore.mvc.cors",
                "microsoft.aspnetcore.mvc.dataannotations",
                "microsoft.aspnetcore.mvc.formatters.json",
                "microsoft.aspnetcore.mvc.localization",
                "microsoft.aspnetcore.mvc.razor",
                "microsoft.aspnetcore.mvc.razor.host",
                "microsoft.aspnetcore.mvc.taghelpers",
                "microsoft.aspnetcore.mvc.viewfeatures",
                "microsoft.aspnetcore.razor",
                "microsoft.aspnetcore.razor.runtime",
                "microsoft.aspnetcore.responsecaching.abstractions",
                "microsoft.aspnetcore.routing",
                "microsoft.aspnetcore.routing.abstractions",
                "microsoft.aspnetcore.server.iisintegration",
                "microsoft.aspnetcore.server.kestrel",
                "microsoft.aspnetcore.staticfiles",
                "microsoft.aspnetcore.webutilities",
                "microsoft.codeanalysis.analyzers",
                "microsoft.codeanalysis.common",
                "microsoft.codeanalysis.csharp",
                "microsoft.codeanalysis.csharp.workspaces",
                "microsoft.codeanalysis.visualbasic",
                "microsoft.codeanalysis.workspaces.common",
                "microsoft.composition",
                "microsoft.csharp",
                "microsoft.data.sqlite",
                "microsoft.diasymreader.native",
                "microsoft.dotnet.internalabstractions",
                "microsoft.dotnet.platformabstractions",
                "microsoft.entityframeworkcore",
                "microsoft.entityframeworkcore.design",
                "microsoft.entityframeworkcore.relational",
                "microsoft.entityframeworkcore.relational.design",
                "microsoft.entityframeworkcore.sqlite",
                "microsoft.entityframeworkcore.sqlite.design",
                "microsoft.entityframeworkcore.sqlserver",
                "microsoft.entityframeworkcore.tools",
                "microsoft.extensions.caching.abstractions",
                "microsoft.extensions.caching.memory",
                "microsoft.extensions.commandlineutils",
                "microsoft.extensions.configuration",
                "microsoft.extensions.configuration.abstractions",
                "microsoft.extensions.configuration.binder",
                "microsoft.extensions.configuration.environmentvariables",
                "microsoft.extensions.configuration.fileextensions",
                "microsoft.extensions.configuration.json",
                "microsoft.extensions.configuration.usersecrets",
                "microsoft.extensions.dependencyinjection",
                "microsoft.extensions.dependencyinjection.abstractions",
                "microsoft.extensions.dependencymodel",
                "microsoft.extensions.fileproviders.abstractions",
                "microsoft.extensions.fileproviders.composite",
                "microsoft.extensions.fileproviders.physical",
                "microsoft.extensions.filesystemglobbing",
                "microsoft.extensions.globalization.cultureinfocache",
                "microsoft.extensions.localization",
                "microsoft.extensions.localization.abstractions",
                "microsoft.extensions.logging",
                "microsoft.extensions.logging.abstractions",
                "microsoft.extensions.logging.console",
                "microsoft.extensions.logging.debug",
                "microsoft.extensions.objectpool",
                "microsoft.extensions.options",
                "microsoft.extensions.options.configurationextensions",
                "microsoft.extensions.platformabstractions",
                "microsoft.extensions.primitives",
                "microsoft.extensions.webencoders",
                "microsoft.net.http.headers",
                "microsoft.netcore.app",
                "microsoft.netcore.dotnethost",
                "microsoft.netcore.dotnethostpolicy",
                "microsoft.netcore.dotnethostresolver",
                "microsoft.netcore.jit",
                "microsoft.netcore.platforms",
                "microsoft.netcore.runtime.coreclr",
                "microsoft.netcore.targets",
                "microsoft.netcore.windows.apisets",
                "microsoft.visualbasic",
                "microsoft.visualstudio.web.codegeneration",
                "microsoft.visualstudio.web.codegeneration.core",
                "microsoft.visualstudio.web.codegeneration.design",
                "microsoft.visualstudio.web.codegeneration.entityframeworkcore",
                "microsoft.visualstudio.web.codegeneration.templating",
                "microsoft.visualstudio.web.codegeneration.utils",
                "microsoft.visualstudio.web.codegenerators.mvc",
                "microsoft.win32.primitives",
                "microsoft.win32.registry",
                "netstandard.library",
                "newtonsoft.json",
                "nuget.frameworks",
                "remotion.linq",
                "runtime.debian.8-x64.runtime.native.system.security.cryptography.openssl",
                "runtime.fedora.23-x64.runtime.native.system.security.cryptography.openssl",
                "runtime.fedora.24-x64.runtime.native.system.security.cryptography.openssl",
                "runtime.native.system",
                "runtime.native.system.data.sqlclient.sni",
                "runtime.native.system.io.compression",
                "runtime.native.system.net.http",
                "runtime.native.system.net.security",
                "runtime.native.system.security.cryptography",
                "runtime.native.system.security.cryptography.apple",
                "runtime.native.system.security.cryptography.openssl",
                "runtime.opensuse.13.2-x64.runtime.native.system.security.cryptography.openssl",
                "runtime.opensuse.42.1-x64.runtime.native.system.security.cryptography.openssl",
                "runtime.osx.10.10-x64.runtime.native.system.security.cryptography.apple",
                "runtime.osx.10.10-x64.runtime.native.system.security.cryptography.openssl",
                "runtime.rhel.7-x64.runtime.native.system.security.cryptography.openssl",
                "runtime.ubuntu.14.04-x64.runtime.native.system.security.cryptography.openssl",
                "runtime.ubuntu.16.04-x64.runtime.native.system.security.cryptography.openssl",
                "runtime.ubuntu.16.10-x64.runtime.native.system.security.cryptography.openssl",
                "runtime.win7-x64.runtime.native.system.data.sqlclient.sni",
                "runtime.win7-x86.runtime.native.system.data.sqlclient.sni",
                "sqlite",
                "system.appcontext",
                "system.buffers",
                "system.collections",
                "system.collections.concurrent",
                "system.collections.immutable",
                "system.collections.nongeneric",
                "system.collections.specialized",
                "system.componentmodel",
                "system.componentmodel.annotations",
                "system.componentmodel.primitives",
                "system.componentmodel.typeconverter",
                "system.console",
                "system.data.common",
                "system.data.sqlclient",
                "system.diagnostics.contracts",
                "system.diagnostics.debug",
                "system.diagnostics.diagnosticsource",
                "system.diagnostics.fileversioninfo",
                "system.diagnostics.process",
                "system.diagnostics.stacktrace",
                "system.diagnostics.tools",
                "system.diagnostics.tracing",
                "system.dynamic.runtime",
                "system.globalization",
                "system.globalization.calendars",
                "system.globalization.extensions",
                "system.interactive.async",
                "system.io",
                "system.io.compression",
                "system.io.compression.zipfile",
                "system.io.filesystem",
                "system.io.filesystem.primitives",
                "system.io.filesystem.watcher",
                "system.io.memorymappedfiles",
                "system.io.pipes",
                "system.io.unmanagedmemorystream",
                "system.linq",
                "system.linq.expressions",
                "system.linq.parallel",
                "system.linq.queryable",
                "system.net.http",
                "system.net.nameresolution",
                "system.net.primitives",
                "system.net.requests",
                "system.net.security",
                "system.net.sockets",
                "system.net.webheadercollection",
                "system.net.websockets",
                "system.numerics.vectors",
                "system.objectmodel",
                "system.reflection",
                "system.reflection.dispatchproxy",
                "system.reflection.emit",
                "system.reflection.emit.ilgeneration",
                "system.reflection.emit.lightweight",
                "system.reflection.extensions",
                "system.reflection.metadata",
                "system.reflection.primitives",
                "system.reflection.typeextensions",
                "system.resources.reader",
                "system.resources.resourcemanager",
                "system.runtime",
                "system.runtime.compilerservices.unsafe",
                "system.runtime.extensions",
                "system.runtime.handles",
                "system.runtime.interopservices",
                "system.runtime.interopservices.runtimeinformation",
                "system.runtime.loader",
                "system.runtime.numerics",
                "system.runtime.serialization.primitives",
                "system.security.claims",
                "system.security.cryptography.algorithms",
                "system.security.cryptography.cng",
                "system.security.cryptography.csp",
                "system.security.cryptography.encoding",
                "system.security.cryptography.openssl",
                "system.security.cryptography.primitives",
                "system.security.cryptography.x509certificates",
                "system.security.principal",
                "system.security.principal.windows",
                "system.text.encoding",
                "system.text.encoding.codepages",
                "system.text.encoding.extensions",
                "system.text.encodings.web",
                "system.text.regularexpressions",
                "system.threading",
                "system.threading.overlapped",
                "system.threading.tasks",
                "system.threading.tasks.dataflow",
                "system.threading.tasks.extensions",
                "system.threading.tasks.parallel",
                "system.threading.thread",
                "system.threading.threadpool",
                "system.threading.timer",
                "system.xml.readerwriter",
                "system.xml.xdocument",
                "system.xml.xmldocument",
                "system.xml.xpath",
                "system.xml.xpath.xdocument"
            };

            _nugetCacheFolder
                .Should()
                .HaveDirectories(expectedDirectories);

            _nugetCacheFolder
                .GetDirectory("system.runtime")
                .Should().HaveDirectories(new string[] { "4.1.0", "4.3.0" });

            _nugetCacheFolder
                .GetDirectory("microsoft.aspnetcore.mvc")
                .Should().HaveDirectories(new string[] { "1.0.6", "1.1.8" });
        }

        private string GetDotnetVersion()
        {
            return new DotnetCommand().ExecuteWithCapturedOutput("--version").StdOut
                .TrimEnd(Environment.NewLine.ToCharArray());
        }

        private static string NormalizeLineEndings(string s)
        {
            return s.Replace("\r\n", "\n").Replace("\r", "\n");
        }
    }
}