using System;
using Microsoft.DotNet.Cli.Utils;

namespace DesktopAppWhichCallsDotnet
{
    public class Program
    {
        public static int Main(string[] args)
        {
            var projectPath = args[0];
            
            return Command.CreateDotNet(
                "build",
                new[] { projectPath })
                .ForwardStdErr()
                .ForwardStdOut()
                .Execute()
                .ExitCode;
        }
    }
}
