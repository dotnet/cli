// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Runtime.InteropServices;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.PlatformAbstractions;
using Microsoft.DotNet.Tools.Common;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.ShellShim
{
    internal class AppHostShellShimMaker : IAppHostShellShimMaker
    {
        private const string ApphostNameWithoutExtension = "apphost";
        private readonly string _appHostSourceDirectory;

        public AppHostShellShimMaker(string appHostSourceDirectory = null)
        {
            _appHostSourceDirectory = appHostSourceDirectory ?? Path.Combine(ApplicationEnvironment.ApplicationBasePath,
                    "AppHostTemplate");
        }
        public void CreateApphostShellShim(FilePath entryPoint, FilePath shimPath)
        {
            string appHostSourcePath;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                appHostSourcePath = Path.Combine(_appHostSourceDirectory, ApphostNameWithoutExtension + ".exe");
            }
            else
            {
                appHostSourcePath = Path.Combine(_appHostSourceDirectory, ApphostNameWithoutExtension);
            }

            var appHostDestinationFilePath = shimPath.Value;
            var appBinaryFilePath = PathUtility.GetRelativePath(appHostDestinationFilePath, entryPoint.Value);

            EmbedAppNameInHost.EmbedAndReturnModifiedAppHostPath(
                appHostSourceFilePath: appHostSourcePath,
                appHostDestinationFilePath: appHostDestinationFilePath,
                appBinaryFilePath: appBinaryFilePath);
        }
    }
}
