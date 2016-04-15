// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Microsoft.DotNet.Cli.Utils
{
    /// <summary>
    /// A Command that is capable of running in the current process.
    /// </summary>
    public class BuiltInCommand : ICommand
    {
        private Action<string> _outputLineHandler;
        private Action<string> _errorLineHandler;
        private IEnumerable<string> _commandArgs;
        private Func<string[], int> _builtInCommand;

        public string CommandName { get; }
        public string CommandArgs => string.Join(" ", _commandArgs);

        public BuiltInCommand(string commandName, IEnumerable<string> commandArgs, Func<string[], int> builtInCommand)
        {
            CommandName = commandName;
            _commandArgs = commandArgs;
            _builtInCommand = builtInCommand;
        }

        public CommandResult Execute()
        {
            TextWriter originalConsoleOut = null;
            TextWriter originalConsoleError = null;

            try
            {
                if (_outputLineHandler != null)
                {
                    originalConsoleOut = Console.Out;

                    var outputWriter = new LineNotificationTextWriter(originalConsoleOut.FormatProvider, originalConsoleOut.Encoding)
                        .OnWriteLine(_outputLineHandler);

                    Console.SetOut(outputWriter);
                }

                if (_errorLineHandler != null)
                {
                    originalConsoleError = Console.Error;

                    var errorWriter = new LineNotificationTextWriter(originalConsoleError.FormatProvider, originalConsoleError.Encoding)
                        .OnWriteLine(_errorLineHandler);

                    Console.SetError(errorWriter);
                }

                if (originalConsoleOut != null || originalConsoleError != null)
                {
                    // Reset the Reporters to the new Console Out and Error.
                    Reporter.Reset();
                }

                int exitCode = _builtInCommand(_commandArgs.ToArray());

                // fake out a ProcessStartInfo using the Muxer command name, since this is a built-in command
                ProcessStartInfo startInfo = new ProcessStartInfo(new Muxer().MuxerPath, $"{CommandName} {CommandArgs}");
                return new CommandResult(startInfo, exitCode, null, null);
            }
            finally
            {
                if (originalConsoleOut != null || originalConsoleError != null)
                {
                    if (originalConsoleOut != null)
                    {
                        Console.SetOut(originalConsoleOut);
                    }

                    if (originalConsoleError != null)
                    {
                        Console.SetError(originalConsoleError);
                    }

                    Reporter.Reset();
                }
            }
        }

        public ICommand OnOutputLine(Action<string> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            if (_outputLineHandler != null)
            {
                throw new InvalidOperationException("OnOutputLine has already been set.");
            }

            _outputLineHandler = handler;

            return this;
        }

        public ICommand OnErrorLine(Action<string> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            if (_errorLineHandler != null)
            {
                throw new InvalidOperationException("OnErrorLine has already been set.");
            }

            _errorLineHandler = handler;

            return this;
        }

        public CommandResolutionStrategy ResolutionStrategy
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public ICommand CaptureStdErr()
        {
            throw new NotImplementedException();
        }

        public ICommand CaptureStdOut()
        {
            throw new NotImplementedException();
        }

        public ICommand EnvironmentVariable(string name, string value)
        {
            throw new NotImplementedException();
        }

        public ICommand ForwardStdErr(TextWriter to = null, bool onlyIfVerbose = false, bool ansiPassThrough = true)
        {
            throw new NotImplementedException();
        }

        public ICommand ForwardStdOut(TextWriter to = null, bool onlyIfVerbose = false, bool ansiPassThrough = true)
        {
            throw new NotImplementedException();
        }

        public ICommand WorkingDirectory(string projectDirectory)
        {
            throw new NotImplementedException();
        }
    }
}
