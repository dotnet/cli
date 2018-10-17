﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.PlatformAbstractions;

namespace Microsoft.DotNet.CommandFactory
{
    public class DotnetToolsCommandResolver : ICommandResolver
    {
        private string _dotnetToolPath;

        public DotnetToolsCommandResolver(string dotnetToolPath = null)
        {
            if (dotnetToolPath == null)
            {
                _dotnetToolPath = Path.Combine(ApplicationEnvironment.ApplicationBasePath,
                    "DotnetTools");
            }
            else
            {
                _dotnetToolPath = dotnetToolPath;
            }
        }

        public CommandSpec Resolve(CommandResolverArguments arguments)
        {
            if (string.IsNullOrEmpty(arguments.CommandName))
            {
                return null;
            }

            var packageId = new DirectoryInfo(Path.Combine(_dotnetToolPath, arguments.CommandName));
            if (!packageId.Exists)
            {
                return null;
            }

            var version = packageId.GetDirectories()[0];
            var dll = version.GetDirectories("tools")[0]
                .GetDirectories()[0] // TFM
                .GetDirectories()[0] // RID
                .GetFiles($"{arguments.CommandName}.dll")[0];

            return MuxerCommandSpecMaker.CreatePackageCommandSpecUsingMuxer(
                    dll.FullName,
                    arguments.CommandArguments);
        }
    }
}
