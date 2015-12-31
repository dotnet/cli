﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using NuGet.Frameworks;

namespace Microsoft.DotNet.Cli.Utils
{
    internal class Command
    {
        private readonly Process _process;
        private readonly StreamForwarder _stdOut;
        private readonly StreamForwarder _stdErr;

        private bool _running = false;

        private Command(ProcessStartInfo psi, CommandResolutionStrategy resolutionStrategy)
        {
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;

            _stdOut = new StreamForwarder();
            _stdErr = new StreamForwarder();
        
            _process = new Process
            {
                StartInfo = psi
            };

            ResolutionStrategy = resolutionStrategy;
        }

        public static Command Create(string commandName, IEnumerable<string> args, NuGetFramework framework = null)
        {
            return Create(commandName, string.Join(" ", args), framework);
        }

        public static Command Create(string commandName, string args, NuGetFramework framework = null)
        {
            var psi = CommandResolver.TryResolvePathAndArgs(commandName, args, framework);

            if (psi == null)
            {
                throw new CommandUnknownException($"No executable found matching command \"{commandName}\"");
            }
            
            var resolutionStrategy = CommandResolutionStrategy.None;

            var command = new Command(psi, resolutionStrategy);

            return command;
        }
        
        public CommandResult Execute()
        {
            Reporter.Verbose.WriteLine($"Running {_process.StartInfo.FileName} {_process.StartInfo.Arguments}");

            ThrowIfRunning();
            _running = true;

            _process.EnableRaisingEvents = true;

#if DEBUG
            var sw = Stopwatch.StartNew();
            Reporter.Verbose.WriteLine($"> {FormatProcessInfo(_process.StartInfo)}".White());
#endif
            _process.Start();

            Reporter.Verbose.WriteLine($"Process ID: {_process.Id}");

            var threadOut = _stdOut.BeginRead(_process.StandardOutput);
            var threadErr = _stdErr.BeginRead(_process.StandardError);

            _process.WaitForExit();
            threadOut.Join();
            threadErr.Join();

            var exitCode = _process.ExitCode;

#if DEBUG
            var message = $"< {FormatProcessInfo(_process.StartInfo)} exited with {exitCode} in {sw.ElapsedMilliseconds} ms.";
            if (exitCode == 0)
            {
                Reporter.Verbose.WriteLine(message.Green());
            }
            else
            {
                Reporter.Verbose.WriteLine(message.Red().Bold());
            }
#endif

            return new CommandResult(
                exitCode,
                _stdOut.GetCapturedOutput(),
                _stdErr.GetCapturedOutput());
        }

        public Command WorkingDirectory(string projectDirectory)
        {
            _process.StartInfo.WorkingDirectory = projectDirectory;
            return this;
        }

        public Command EnvironmentVariable(string name, string value)
        {
            _process.StartInfo.Environment[name] = value;
            return this;
        }

        public Command CaptureStdOut()
        {
            ThrowIfRunning();
            _stdOut.Capture();
            return this;
        }

        public Command CaptureStdErr()
        {
            ThrowIfRunning();
            _stdErr.Capture();
            return this;
        }

        public Command ForwardStdOut(TextWriter to = null, bool onlyIfVerbose = false)
        {
            ThrowIfRunning();
            if (!onlyIfVerbose || CommandContext.IsVerbose())
            {
                if (to == null)
                {
                    _stdOut.ForwardTo(write: Reporter.Output.Write, writeLine: Reporter.Output.WriteLine);
                }
                else
                {
                    _stdOut.ForwardTo(write: to.Write, writeLine: to.WriteLine);
                }
            }
            return this;
        }

        public Command ForwardStdErr(TextWriter to = null, bool onlyIfVerbose = false)
        {
            ThrowIfRunning();
            if (!onlyIfVerbose || CommandContext.IsVerbose())
            {
                if (to == null)
                {
                    _stdErr.ForwardTo(write: Reporter.Error.Write, writeLine: Reporter.Error.WriteLine);
                }
                else
                {
                    _stdErr.ForwardTo(write: to.Write, writeLine: to.WriteLine);
                }
            }
            return this;
        }

        public Command OnOutputLine(Action<string> handler)
        {
            ThrowIfRunning();
            _stdOut.ForwardTo(write: null, writeLine: handler);
            return this;
        }

        public Command OnErrorLine(Action<string> handler)
        {
            ThrowIfRunning();
            _stdErr.ForwardTo(write: null, writeLine: handler);
            return this;
        }

        public CommandResolutionStrategy ResolutionStrategy { get; }

        public string CommandName => _process.StartInfo.FileName;

        private string FormatProcessInfo(ProcessStartInfo info)
        {
            if (string.IsNullOrWhiteSpace(info.Arguments))
            {
                return info.FileName;
            }

            return info.FileName + " " + info.Arguments;
        }

        private void ThrowIfRunning([CallerMemberName] string memberName = null)
        {
            if (_running)
            {
                throw new InvalidOperationException($"Unable to invoke {memberName} after the command has been run");
            }
        }
    }
}
