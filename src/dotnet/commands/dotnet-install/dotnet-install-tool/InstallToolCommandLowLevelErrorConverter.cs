﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.DotNet.ShellShim;
using Microsoft.DotNet.ToolPackage;

namespace Microsoft.DotNet.Tools.Install.Tool
{
    internal static class InstallToolCommandLowLevelErrorConverter
    {
        public static IEnumerable<string> GetUserFacingMessages(Exception ex, PackageId packageId)
        {
            string[] userFacingMessages = null;
            if (ex is ToolPackageException)
            {
                userFacingMessages = new[]
                {
                    string.Format(
                        LocalizableStrings.InvalidToolConfiguration,
                        ex.Message),
                    string.Format(LocalizableStrings.ToolInstallationFailedContactAuthor, packageId)
                };
            }
            else if (ex is ToolConfigurationException)
            {
                userFacingMessages = new[]
                {
                    string.Format(
                        LocalizableStrings.InvalidToolConfiguration,
                        ex.Message),
                    string.Format(LocalizableStrings.ToolInstallationFailedContactAuthor, packageId)
                };
            }
            else if (ex is ShellShimException)
            {
                userFacingMessages = new[]
                {
                    string.Format(
                        LocalizableStrings.FailedToCreateToolShim,
                        packageId,
                        ex.Message),
                    string.Format(LocalizableStrings.ToolInstallationFailed, packageId)
                };
            }

            return userFacingMessages;
        }

        public static bool ShouldConvertToUserFacingError(Exception ex)
        {
            return ex is ToolPackageException
                   || ex is ToolConfigurationException
                   || ex is ShellShimException;
        }
    }
}
