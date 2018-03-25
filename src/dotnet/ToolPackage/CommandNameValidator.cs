// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.ToolPackage
{
    public class CommandNameValidator
    {
        private string _agaist;
        private bool _blockWhenContainAnywhere;
        private bool _blockWhenStartWithWord;
        private bool _blockWhenMatchWhole;


        public CommandNameValidator(
            bool blockWhenContainAnywhere,
            bool blockWhenStartWith,
            bool blockWhenMatchWhole, string agaist)
        {
            _agaist = agaist;
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
                if (commandName.IndexOf(_agaist, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return new[] {
                        string.Format("command name '{0}' contains reserved string '{1}'.", commandName, _agaist)
                    };
                }
            }
            else if (_blockWhenStartWithWord)
            {
                if (commandName.StartsWith(_agaist + "-", StringComparison.OrdinalIgnoreCase) ||
                    commandName.Equals(_agaist, StringComparison.OrdinalIgnoreCase))
                {
                    return new[] {
                        string.Format("command name '{0}' starts with reserved word '{1}'.", commandName, _agaist)
                    };
                }
            }
            else if (_blockWhenMatchWhole)
            {
                if (commandName.Equals(_agaist, StringComparison.OrdinalIgnoreCase))
                {
                    return new[] {
                        string.Format("command name '{0}' matches reserved string '{1}'.", commandName, _agaist)
                    };
                }
            }

            return new string[0];
        }
    }
}
