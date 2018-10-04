﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using NuGet.Frameworks;

namespace Microsoft.DotNet.Cli.Utils
{
    public class Command : ICommand
    {
        private readonly Process _process;

        private StreamForwarder _stdOut;
        
        private StreamForwarder _stdErr;

        private bool _running = false;

        public Command(Process process)
        {
            _process = process ?? throw new ArgumentNullException(nameof(process));
        }

        public static Command Create(string path, IEnumerable<string> args)
        {
            var psi = new ProcessStartInfo
            {
                FileName = path,
                Arguments = ArgumentEscaper.EscapeAndConcatenateArgArrayForProcessStart(args),
                UseShellExecute = false
            };


            var _process = new Process
            {
                StartInfo = psi
            };

            return new Command(_process);
        }

        public CommandResult Execute()
        {

            Reporter.Verbose.WriteLine(string.Format(
                LocalizableStrings.RunningFileNameArguments,
                _process.StartInfo.FileName,
                _process.StartInfo.Arguments));

            ThrowIfRunning();

            _running = true;

            _process.EnableRaisingEvents = true;

#if DEBUG
            var sw = Stopwatch.StartNew();
            
            Reporter.Verbose.WriteLine($"> {FormatProcessInfo(_process.StartInfo)}".White());
#endif
            using (PerfTrace.Current.CaptureTiming($"{Path.GetFileNameWithoutExtension(_process.StartInfo.FileName)} {_process.StartInfo.Arguments}"))
            {
                _process.Start();

                Reporter.Verbose.WriteLine(string.Format(
                    LocalizableStrings.ProcessId,
                    _process.Id));

                var taskOut = _stdOut?.BeginRead(_process.StandardOutput);
                var taskErr = _stdErr?.BeginRead(_process.StandardError);
                _process.WaitForExit();

                taskOut?.Wait();
                taskErr?.Wait();
            }

            var exitCode = _process.ExitCode;

#if DEBUG
            var message = string.Format(
                LocalizableStrings.ProcessExitedWithCode,
                FormatProcessInfo(_process.StartInfo),
                exitCode,
                sw.ElapsedMilliseconds);
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
                _process.StartInfo,
                exitCode,
                _stdOut?.CapturedOutput,
                _stdErr?.CapturedOutput);
        }

        public ICommand WorkingDirectory(string projectDirectory)
        {
            _process.StartInfo.WorkingDirectory = projectDirectory;
            return this;
        }

        public ICommand EnvironmentVariable(string name, string value)
        {
#if NET451
            _process.StartInfo.EnvironmentVariables[name] = value;
#else
            _process.StartInfo.Environment[name] = value;
#endif
            return this;
        }

        public ICommand CaptureStdOut()
        {
            ThrowIfRunning();
            EnsureStdOut();
            _stdOut.Capture();
            return this;
        }

        public ICommand CaptureStdErr()
        {
            ThrowIfRunning();
            EnsureStdErr();
            _stdErr.Capture();
            return this;
        }

        public ICommand ForwardStdOut(TextWriter to = null, bool onlyIfVerbose = false, bool ansiPassThrough = true)
        {
            ThrowIfRunning();
            if (!onlyIfVerbose || CommandContext.IsVerbose())
            {
                EnsureStdOut();

                if (to == null)
                {
                    _stdOut.ForwardTo(writeLine: Reporter.Output.WriteLine);
                    EnvironmentVariable(CommandContext.Variables.AnsiPassThru, ansiPassThrough.ToString());
                }
                else
                {
                    _stdOut.ForwardTo(writeLine: to.WriteLine);
                }
            }
            return this;
        }

        public ICommand ForwardStdErr(TextWriter to = null, bool onlyIfVerbose = false, bool ansiPassThrough = true)
        {
            ThrowIfRunning();
            if (!onlyIfVerbose || CommandContext.IsVerbose())
            {
                EnsureStdErr();

                if (to == null)
                {
                    _stdErr.ForwardTo(writeLine: Reporter.Error.WriteLine);
                    EnvironmentVariable(CommandContext.Variables.AnsiPassThru, ansiPassThrough.ToString());
                }
                else
                {
                    _stdErr.ForwardTo(writeLine: to.WriteLine);
                }
            }
            return this;
        }

        public ICommand OnOutputLine(Action<string> handler)
        {
            ThrowIfRunning();
            EnsureStdOut();

            _stdOut.ForwardTo(writeLine: handler);
            return this;
        }

        public ICommand OnErrorLine(Action<string> handler)
        {
            ThrowIfRunning();
            EnsureStdErr();

            _stdErr.ForwardTo(writeLine: handler);
            return this;
        }

        public string CommandName => _process.StartInfo.FileName;

        public string CommandArgs => _process.StartInfo.Arguments;

        private string FormatProcessInfo(ProcessStartInfo info)
        {
            if (string.IsNullOrWhiteSpace(info.Arguments))
            {
                return info.FileName;
            }

            return info.FileName + " " + info.Arguments;
        }

        private void EnsureStdOut()
        {
            _stdOut = _stdOut ?? new StreamForwarder();
            _process.StartInfo.RedirectStandardOutput = true;
        }

        private void EnsureStdErr()
        {
            _stdErr = _stdErr ?? new StreamForwarder();
            _process.StartInfo.RedirectStandardError = true;
        }

        private void ThrowIfRunning([CallerMemberName] string memberName = null)
        {
            if (_running)
            {
                throw new InvalidOperationException(string.Format(
                    LocalizableStrings.UnableToInvokeMemberNameAfterCommand,
                    memberName));
            }
        }
    }
}
