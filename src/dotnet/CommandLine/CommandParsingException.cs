﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools;

namespace Microsoft.DotNet.Cli.CommandLine
{
    internal class CommandParsingException : Exception
    {
        private bool _isRequireSubCommandMissing;

        public CommandParsingException(
            CommandLineApplication command,
            string message,
            bool isRequireSubCommandMissing = false)
            : base(message)
        {
            Command = command;
            _isRequireSubCommandMissing = isRequireSubCommandMissing;

            Data.Add("CLI_User_Displayed_Exception", true);
        }

        public CommandLineApplication Command { get; }

        public override string Message
        {
            get
            {
                return _isRequireSubCommandMissing
                     ? CommonLocalizableStrings.RequiredCommandNotPassed
                     : base.Message;
            }
        }
    }
}
