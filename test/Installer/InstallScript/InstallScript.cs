// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Xunit.Abstractions;
using Xunit;

namespace Microsoft.DotNet.InstallScripts.Tests
{
    public class InstallScript
    {
        public static string RepoRoot { get; private set; }
        public static string Shell { get; private set; }
        public static string ShellArgs { get; private set; }
        public static string InstallScriptPath { get; private set; }
        public static string OSName { get; private set; }
        public static string ZipExtension { get; private set; }
        
        public string StdIn { get; private set; }
        public string StdOut { get; private set; }
        public string StdErr { get; private set; }
        public bool TimedOut { get; private set; }
        public int ExitCode { get; private set; }
        
        private ITestOutputHelper _output;
        private Process _process;
        
        static InstallScript()
        {
            RepoRoot = FindRepoRoot();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Shell = "powershell";
                ShellArgs = "-File ";
                InstallScriptPath = Path.Combine(RepoRoot, "scripts", "obtain", "install.ps1");
                ZipExtension = "zip";
            }
            else
            {
                Shell = "bash";
                ShellArgs = "";
                InstallScriptPath = Path.Combine(RepoRoot, "scripts", "obtain", "install.sh");
                ZipExtension = "tar.gz";
            }
            
            OSName = GetOSName();
        }
        
        public InstallScript(ITestOutputHelper output, string additionalArguments, string stdIn = null)
        {
            _output = output;
            StdIn = stdIn;
            _process = PrepareProcess(additionalArguments);
        }
        
        private static string GetOSName()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "win";
            }
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "osx";
            }
            
            foreach (var path in Directory.GetFiles("/etc/", "*-release"))
            {
                string content = File.ReadAllText(path);
                if (content.Contains("ubuntu"))
                {
                    return "ubuntu";
                }
                
                if (content.Contains("centos"))
                {
                    return "centos";
                }
                
                if (content.Contains("rhel"))
                {
                    return "rhel";
                }
                
                if (content.Contains("debian"))
                {
                    return "debian";
                }
            }
            
            throw new Exception("OS not supported");
        }
        
        private static Process PrepareProcess(string additionalArguments)
        {
            var arguments = $"{ShellArgs}\"{InstallScriptPath}\" {additionalArguments}";
            Process ret = new Process();
            ret.StartInfo.FileName = Shell;
            ret.StartInfo.Arguments = arguments;
            ret.StartInfo.UseShellExecute = false;
            
            ret.StartInfo.RedirectStandardInput = true;
            ret.StartInfo.RedirectStandardOutput = true;
            ret.StartInfo.RedirectStandardError = true;
            
            return ret;
        }
        
        public void StartAndWait()
        {
            _output.WriteLine($"Calling {Shell} {_process.StartInfo.Arguments}");
            _process.Start();
            
            if (!string.IsNullOrEmpty(StdIn))
            {
                _process.StandardInput.Write(StdIn);
                _process.StandardInput.Flush();
            }
            
            int timeout = (_process.StartInfo.Arguments.Contains("-DryRun")) ? (10 * 1000) : (5 * 60 * 1000);
            
            TimedOut = !_process.WaitForExit(timeout);
            
            _output.WriteLine("Process stdout:");
            StdOut = _process.StandardOutput.ReadToEnd();
            _output.WriteLine(StdOut);
            _output.WriteLine("Process stderr:");
            StdErr = _process.StandardError.ReadToEnd();
            _output.WriteLine(StdErr);
            
            ExitCode = _process.ExitCode;
        }
        
        public static InstallScript Install(ITestOutputHelper output, string additionalArguments, string stdIn = null)
        {
            var ret = new InstallScript(output, additionalArguments, stdIn);
            ret.StartAndWait();
            return ret;
        }
        
        private static string NormalizeArchitecture(string architecture)
        {
            switch (architecture)
            {
                case "AMD64":
                    architecture = "x64";
                    break;
                case "x64":
                case "x86":
                    break;
                default:
                    /* Intentionally not failing */
                    break;
            }
            
            return architecture;
        }
        
        private static string NormalizeChannel(string channel)
        {
            switch (channel)
            {
                case "future": channel = "dev"; break;
                case "preview": channel = "beta"; break;
                default:
                    /* Intentionally not failing */
                    break;
            }
            
            return channel;
        }
        
        public static string VersionFilePath(string architecture, string channel = "preview")
        {
            architecture = NormalizeArchitecture(architecture);
            channel = NormalizeChannel(channel);
            return $"/{channel}/dnvm/latest.{OSName}.{architecture}.version";
        }
        
        public static string DotnetDevZipPath(string version, string architecture, string channel = "preview")
        {
            architecture = NormalizeArchitecture(architecture);
            channel = NormalizeChannel(channel);
            return $"/{channel}/Binaries/{version}/dotnet-dev-{OSName}-{architecture}.{version}.{ZipExtension}";
        }
        
        static string FindRepoRoot()
        {
            string directory = AppContext.BaseDirectory;
            while (!Directory.Exists(Path.Combine(directory, ".git")) && directory != null)
            {
                directory = Directory.GetParent(directory).FullName;
            }

            if (directory == null)
            {
                throw new Exception("Cannot find the git repository root");
            }
            
            return directory;
        }
    }
}