using System;
using System.IO;
using Microsoft.DotNet.Cli.Build.Framework;

using static Microsoft.DotNet.Cli.Build.Framework.BuildHelpers;

namespace Microsoft.DotNet.Scripts
{
    public static class PushPRTargets
    {
        [Target(nameof(CommitChanges), nameof(CreatePR))]
        public static BuildTargetResult PushPR(BuildTargetContext c) => c.Success();

        [Target]
        public static BuildTargetResult CommitChanges(BuildTargetContext c)
        {
            Cmd("git", "add", ".")
                .Execute()
                .EnsureSuccessful();

            Cmd("git", "commit", "-m", "Updating dependencies")
                .Execute()
                .EnsureSuccessful();

            string remoteBranchName = $"UpdateDependencies-{DateTime.UtcNow.ToString("YYYYMMDD-HHmmSS")}";
            Cmd("git", "push", "https://github.com/eerhardt/cli.git", $"HEAD:{remoteBranchName}")
                .Execute()
                .EnsureSuccessful();

            c.BuildContext["RemoteBranchName"] = remoteBranchName;

            return c.Success();
        }

        [Target]
        public static BuildTargetResult CreatePR(BuildTargetContext c)
        {
            return c.Success();
        }
    }
}
