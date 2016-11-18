using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Tools;

namespace Microsoft.DotNet.Tools.RestoreProjectJson
{
    internal static class NuGet3
    {
        public static int Restore(IEnumerable<string> args)
        {
            var prefixArgs = new List<string>();
            if (!args.Any(s => s.Equals("--verbosity", StringComparison.OrdinalIgnoreCase) || s.Equals("-v", StringComparison.OrdinalIgnoreCase)))
            {
                prefixArgs.Add("--verbosity");
                prefixArgs.Add(LocalizableStrings.AddMinimal);
            }
            prefixArgs.Add(LocalizableStrings.AddRestore);

            var nugetApp = new NuGetForwardingApp(Enumerable.Concat(prefixArgs, args));

            // setting NUGET_XPROJ_WRITE_TARGETS will tell nuget restore to install .props and .targets files
            // coming from NuGet packages
            const string nugetXProjWriteTargets = "NUGET_XPROJ_WRITE_TARGETS";
            bool setXProjWriteTargets = Environment.GetEnvironmentVariable(nugetXProjWriteTargets) == null;
            if (setXProjWriteTargets)
            {
                nugetApp.WithEnvironmentVariable(nugetXProjWriteTargets, "true");
            }

            return nugetApp.Execute();
        }
    }
}
