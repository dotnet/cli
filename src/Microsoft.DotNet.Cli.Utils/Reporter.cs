﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.DotNet.Cli.Utils
{
    // Stupid-simple console manager
    public class Reporter
    {
        private static readonly Reporter Null = new Reporter(console: null);
        private static object _lock = new object();

        private readonly AnsiConsole _console;

        private Reporter(AnsiConsole console)
        {
            _console = console;
        }

        public static Reporter Output { get; } = Create(AnsiConsole.GetOutput);
        public static Reporter Error { get; } = Create(AnsiConsole.GetError);
        public static Reporter Verbose { get; } = CommandContext.IsVerbose() ? Create(AnsiConsole.GetOutput) : Null;

        public static Reporter Create(Func<bool, AnsiConsole> getter)
        {
            return new Reporter(getter(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)));
        }

        public void WriteLine(string message)
        {
            lock (_lock)
            {
                if (CommandContext.ShouldPassAnsiCodesThrough())
                {
                    _console?.Writer?.WriteLine(message);
                }
                else
                {
                    _console?.WriteLine(message);
                }
            }
        }

        public void WriteLine()
        {
            lock (_lock)
            {
                _console?.Writer?.WriteLine();
            }
        }

        public void Write(string message)
        {
            lock (_lock)
            {
                _console?.Writer?.Write(message);
            }
        }
    }
}
