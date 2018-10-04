using System.Diagnostics;
using Microsoft.DotNet.Cli.Utils;

namespace Microsoft.DotNet.TestFramework
{
    public static class TestCommandFactory
    {
        public static Command Create(string path, string[] args)
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
    }
}
