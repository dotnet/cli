﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.DotNet.Cli.Utils;
using NuGet.Frameworks;

namespace Microsoft.DotNet.CommandFactory
{
    public class PublishedPathCommandFactory : ICommandFactory
    {
        private readonly string _publishDirectory;
        private readonly string _applicationName;

        public PublishedPathCommandFactory(string publishDirectory, string applicationName)
        {
            _publishDirectory = publishDirectory;
            _applicationName = applicationName;
        }

        public ICommand Create(
            string commandName,
            IEnumerable<string> args,
            NuGetFramework framework = null,
            string configuration = Constants.DefaultConfiguration)
        {
            return CommandFactory2.Create(commandName, args, framework, configuration, _publishDirectory, _applicationName);
        }
    }
}
