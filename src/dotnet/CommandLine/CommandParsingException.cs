﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Tools;

namespace Microsoft.DotNet.Cli.CommandLine
{
    internal class CommandParsingException : Exception
    {
        private readonly bool _isRequireSubCommandMissing;

        public CommandParsingException(
            string message, 
            string helpText = null) : base(message)
        {
            HelpText = helpText ?? "";
            Data.Add("CLI_User_Displayed_Exception", true);
        }

        public CommandParsingException(
            CommandLineApplication command,
            string message,
            bool isRequireSubCommandMissing = false)
            : this(message)
        {
            Command = command;
            _isRequireSubCommandMissing = isRequireSubCommandMissing;
        }

        public CommandLineApplication Command { get; }

        public string HelpText { get; } = "";

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