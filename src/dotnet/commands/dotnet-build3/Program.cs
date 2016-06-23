using Microsoft.DotNet.Cli.Utils;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Microsoft.DotNet.Cli
{
    public class Build3Command
    {
        public static int Run(string[] args)
        {
            return new MSBuildForwardingApp(args).Execute();
        }
        
    }
}
