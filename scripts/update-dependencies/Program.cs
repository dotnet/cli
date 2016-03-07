using Microsoft.DotNet.Cli.Build.Framework;

namespace Microsoft.DotNet.Scripts
{
    public class Program
    {
        public static int Main(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);

            return BuildSetup.Create(".NET CLI Dependency Updater")
                .UseTargets(new[]
                {
                    new BuildTarget("Default", "Dependency Updater Goals", new [] { "UpdateFiles", "PushPR" }),
                    new BuildTarget("UpdateFiles", "Dependency Updater Goals"),
                    new BuildTarget("PushPR", "Dependency Updater Goals"),
                })
                .UseAllTargetsFromAssembly<Program>()
                .Run(args);
        }
    }
}
