// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Tools;

namespace Microsoft.DotNet.ToolPackage
{
    public class CommandNameValidator
    {
        private string _reserved;
        private bool _blockWhenContainAnywhere;
        private bool _blockWhenStartWithWord;
        private bool _blockWhenMatchWhole;


        public CommandNameValidator(
            bool blockWhenContainAnywhere,
            bool blockWhenStartWith,
            bool blockWhenMatchWhole,
            string reserved)
        {
            _reserved = reserved;
            _blockWhenContainAnywhere = blockWhenContainAnywhere;
            _blockWhenStartWithWord = blockWhenStartWith;
            _blockWhenMatchWhole = blockWhenMatchWhole;

            if (_blockWhenContainAnywhere == false && _blockWhenStartWithWord == false && _blockWhenMatchWhole == false)
            {
                throw new ArgumentException("This validator doesn't do anything.");
            }
        }

        public string[] GenerateError(string commandName)
        {
            if (_blockWhenContainAnywhere)
            {
                if (commandName.IndexOf(_reserved, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return new[] {
                        string.Format(CommonLocalizableStrings.CommandNameContainsReservedString, commandName, _reserved)
                    };
                }
            }
            else if (_blockWhenStartWithWord)
            {
                if (commandName.StartsWith(_reserved + "-", StringComparison.OrdinalIgnoreCase) ||
                    commandName.Equals(_reserved, StringComparison.OrdinalIgnoreCase))
                {
                    return new[] {
                        string.Format(CommonLocalizableStrings.CommandNameStartsWithReservedString, commandName, _reserved)
                    };
                }
            }
            else if (_blockWhenMatchWhole)
            {
                if (commandName.Equals(_reserved, StringComparison.OrdinalIgnoreCase))
                {
                    return new[] {
                        string.Format(CommonLocalizableStrings.CommandNameMatchesReservedString, commandName, _reserved)
                    };
                }
            }

            return new string[0];
        }
    }
}
