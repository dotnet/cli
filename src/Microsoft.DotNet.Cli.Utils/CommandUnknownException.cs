// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.DotNet.Cli.Utils
{
    public class CommandUnknownException : GracefulException
    {
        public CommandUnknownException(string commandName) : base(string.Format(
            LocalizableStrings.NoExecutableFoundMatchingCommand,
            RemoveLeadingDotnet(commandName), Path.GetFileNameWithoutExtension(commandName)))
        {
        }

        public CommandUnknownException(string commandName, Exception innerException) : base(
            string.Format(
                LocalizableStrings.NoExecutableFoundMatchingCommand,
                RemoveLeadingDotnet(commandName), Path.GetFileNameWithoutExtension(commandName)),
            innerException)
        {
        }

        public static string RemoveLeadingDotnet(string commandName)
        {
            if (commandName.StartsWith("dotnet-", StringComparison.OrdinalIgnoreCase))
            {
                return commandName.Substring("dotnet-".Length);
            }

            return commandName;
        }
    }
}
