using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ProjectModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Cli
{
    /**
     * A class which encapsulates logic needed to forward arguments from the current process to another process
     * invoked with the dotnet.exe host.
     */ 
    public class ForwardingApp
    {
        private static readonly string s_hostExe = "dotnet";

        private readonly string _forwardApplicationPath;
        private readonly string[] _argsToForward;
        private readonly string _depsFile;
        private readonly string _runtimeConfig;
        private readonly string _additionalProbingPath;
        private readonly Dictionary<string, string> _environment;

        private string[] _allArgs;
        
        public ForwardingApp(
            string forwardApplicationPath, 
            string[] argsToForward, 
            string depsFile = null, 
            string runtimeConfig = null,
            string additionalProbingPath = null,
            Dictionary<string, string> environment = null)
        {
            _forwardApplicationPath = forwardApplicationPath;
            _argsToForward = argsToForward;
            _depsFile = depsFile;
            _runtimeConfig = runtimeConfig;
            _additionalProbingPath = additionalProbingPath;
            _environment = environment;
            
            var allArgs = new List<string>();
            allArgs.Add("exec");

            if (_depsFile != null)
            {
                allArgs.Add("--depsfile");
                allArgs.Add(_depsFile);
            }

            if (_runtimeConfig != null)
            {
                allArgs.Add("--runtimeconfig");
                allArgs.Add(_runtimeConfig);
            }

            if (_additionalProbingPath != null)
            {
                allArgs.Add("--additionalprobingpath");
                allArgs.Add(_additionalProbingPath);
            }

            allArgs.Add(_forwardApplicationPath);
            allArgs.AddRange(_argsToForward);

            _allArgs = allArgs.ToArray<string>();
        }

        public int Execute()
        {
            var processInfo = new ProcessStartInfo()
            {
                FileName = GetHostExeName(),
                Arguments = ArgumentEscaper.EscapeAndConcatenateArgArrayForProcessStart(_allArgs),
                UseShellExecute = false
            };

            if (_environment != null)
            {
                foreach (var entry in _environment)
                {
                    processInfo.Environment[entry.Key] = entry.Value;
                }
            }

            var process = new Process()
            {
                StartInfo = processInfo
            };

            process.Start();
            process.WaitForExit();

            return process.ExitCode;
        }

        private string GetHostExeName()
        {
            return $"{s_hostExe}.{FileNameSuffixes.CurrentPlatform.Exe}";
        }
    }
}
