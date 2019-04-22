// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.CommandLine;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ToolManifest;
using Microsoft.DotNet.ToolPackage;
using Microsoft.DotNet.Tools.Tool.Common;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.Tools.Tool.Uninstall
{
    internal class ToolUninstallLocalCommand : CommandBase
    {
        private readonly IToolManifestFinder _toolManifestFinder;
        private readonly IToolManifestEditor _toolManifestEditor;
        private readonly IReporter _reporter;

        private readonly PackageId _packageId;
        private readonly string _explicitManifestFile;

        public ToolUninstallLocalCommand(
            AppliedOption appliedCommand,
            ParseResult parseResult,
            IToolManifestFinder toolManifestFinder = null,
            IToolManifestEditor toolManifestEditor = null,
            IReporter reporter = null)
            : base(parseResult)
        {
            if (appliedCommand == null)
            {
                throw new ArgumentNullException(nameof(appliedCommand));
            }

            _packageId = new PackageId(appliedCommand.Arguments.Single());
            _explicitManifestFile = appliedCommand.SingleArgumentOrDefault(ToolAppliedOption.ToolManifest);

            _reporter = (reporter ?? Reporter.Output);

            _toolManifestFinder = toolManifestFinder ??
                                  new ToolManifestFinder(new DirectoryPath(Directory.GetCurrentDirectory()));
            _toolManifestEditor = toolManifestEditor ?? new ToolManifestEditor();
        }

        public override int Execute()
        {
            (FilePath? manifestFileOptional, string warningMessage) =
                _toolManifestFinder.ExplicitManifestOrFindManifestContainPackageId(_explicitManifestFile, _packageId);

            if (!manifestFileOptional.HasValue)
            {
                throw new GracefulException(
                    new[] { string.Format(LocalizableStrings.NoManifestFileContainPackageId, _packageId) }, 
                    isUserError: false);
            }

            var manifestFile = manifestFileOptional.Value;

            _toolManifestEditor.Remove(manifestFile, _packageId);

            if (warningMessage != null)
            {
                _reporter.WriteLine(warningMessage.Yellow());
            }

            _reporter.WriteLine(
                string.Format(
                    LocalizableStrings.UninstallLocalToolSucceeded,
                    _packageId,
                    manifestFile.Value).Green());
            return 0;
        }
    }
}
