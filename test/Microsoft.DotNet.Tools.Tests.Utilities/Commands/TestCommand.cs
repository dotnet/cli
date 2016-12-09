// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Cli.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Tools.Test.Utilities
{
    public class TestCommand
    {
        protected string _command;

        private string _baseDirectory;

        public string WorkingDirectory { get; set; }

        public Process CurrentProcess { get; private set; }

        public Dictionary<string, string> Environment { get; } = new Dictionary<string, string>();

        private List<Action<string>> _writeLines = new List<Action<string>>();

        private List<string> _cliGeneratedEnvironmentVariables = new List<string> { "MSBuildSDKsPath" };

        public event DataReceivedEventHandler ErrorDataReceived;

        public event DataReceivedEventHandler OutputDataReceived;

        public TestCommand(string command)
        {
            _command = command;

#if NET451            
            _baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
#else
            _baseDirectory = AppContext.BaseDirectory;
#endif 
        }

        public virtual CommandResult Execute(string args = "")
        {
            return ExecuteAsync(args).Result;
        }

        public async virtual Task<CommandResult> ExecuteAsync(string args = "")
        {
            var resolvedCommand = _command;

            ResolveCommand(ref resolvedCommand, ref args);

            Console.WriteLine($"Executing - {resolvedCommand} {args} - {WorkingDirectoryInfo()}");
            
            return await ExecuteAsyncInternal(resolvedCommand, args);
        }

        public virtual CommandResult ExecuteWithCapturedOutput(string args = "")
        {
            var resolvedCommand = _command;

            ResolveCommand(ref resolvedCommand, ref args);

            var commandPath = Env.GetCommandPath(resolvedCommand, ".exe", ".cmd", "") ??
                Env.GetCommandPathFromRootPath(_baseDirectory, resolvedCommand, ".exe", ".cmd", "");

            Console.WriteLine($"Executing (Captured Output) - {commandPath} {args} - {WorkingDirectoryInfo()}");

            return ExecuteAsyncInternal(resolvedCommand, args).Result;
        }

        public void KillTree()
        {
            if (CurrentProcess == null)
            {
                throw new InvalidOperationException("No process is available to be killed");
            }

            CurrentProcess.KillTree();
        }

        private void ResolveCommand(ref string executable, ref string args)
        {
            if (executable.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                var newArgs = ArgumentEscaper.EscapeSingleArg(executable);

                if (!string.IsNullOrEmpty(args))
                {
                    newArgs += " " + args;
                }

                args = newArgs;

                executable = "dotnet";
            }

            if (!Path.IsPathRooted(executable))
            {
                executable = Env.GetCommandPath(executable) ??
                             Env.GetCommandPathFromRootPath(_baseDirectory, executable);
            }
        }


        private async Task<CommandResult> ExecuteAsyncInternal(string executable, string args)
        {
            var stdOut = new List<String>();

            var stdErr = new List<String>();

            CurrentProcess = CreateProcess(executable, args); 

            CurrentProcess.ErrorDataReceived += (s, e) =>
            {
                stdErr.Add(e.Data);

                var handler = ErrorDataReceived;
                
                if (handler != null)
                {
                    handler(s, e);
                }
            };

            CurrentProcess.OutputDataReceived += (s, e) =>
            {
                stdOut.Add(e.Data);

                var handler = OutputDataReceived;
                
                if (handler != null)
                {
                    handler(s, e);
                }
            };

            CurrentProcess.Start();

            CurrentProcess.BeginOutputReadLine();

            CurrentProcess.BeginErrorReadLine();

            var processWaitTask = CurrentProcess.WaitForExitAsync();

            var processTimeoutCancellationTokenSource = new CancellationTokenSource();

            if (await Task.WhenAny(processWaitTask, Task.Delay(120000, processTimeoutCancellationTokenSource.Token)) != processWaitTask)
            {
                KillTree();

                throw new TimeoutException($"Timeout - {executable} {args} - {WorkingDirectoryInfo()}");
            }

            processTimeoutCancellationTokenSource.Cancel();

            CurrentProcess.WaitForExit();

            RemoveNullTerminator(stdOut);

            RemoveNullTerminator(stdErr);

            return new CommandResult(
                CurrentProcess.StartInfo,
                CurrentProcess.ExitCode,
                String.Join(System.Environment.NewLine, stdOut),
                String.Join(System.Environment.NewLine, stdErr));
        }

        private Process CreateProcess(string executable, string args)
        {
            var psi = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = args,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                UseShellExecute = false
            };

            RemoveCliGeneratedEnvironmentVariables(psi);

            foreach (var item in Environment)
            {
#if NET451
                psi.EnvironmentVariables[item.Key] = item.Value;
#else
                psi.Environment[item.Key] = item.Value;
#endif
            }

            if (!string.IsNullOrWhiteSpace(WorkingDirectory))
            {
                psi.WorkingDirectory = WorkingDirectory;
            }

            var process = new Process
            {
                StartInfo = psi
            };

            process.EnableRaisingEvents = true;

            return process;
        }

        private string WorkingDirectoryInfo()
        {
            if (WorkingDirectory == null)
            { 
                return "";
            }

            return $" in pwd {WorkingDirectory}";
        }

        private void RemoveCliGeneratedEnvironmentVariables(ProcessStartInfo psi)
        {
            foreach (var name in _cliGeneratedEnvironmentVariables)
            {
#if NET451
                psi.EnvironmentVariables.Remove(name);
#else
                psi.Environment.Remove(name);
#endif
            }
        }

        private void RemoveNullTerminator(List<string> strings)
        {
            var count = strings.Count;

            if (count < 1)
            {
                return;
            }

            if (strings[count - 1] == null)
            {
                strings.RemoveAt(count - 1);
            }
        }
    }
}