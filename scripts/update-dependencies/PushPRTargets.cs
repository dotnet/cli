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

            string userName = Environment.GetEnvironmentVariable("GITHUB_USER");
            if (string.IsNullOrEmpty(userName))
            {
                return c.Failed("Can't find GITHUB_USER");
            }

            string email = Environment.GetEnvironmentVariable("GITHUB_EMAIL");
            if (string.IsNullOrEmpty(email))
            {
                return c.Failed("Can't find GITHUB_EMAIL");
            }

            string password = Environment.GetEnvironmentVariable("GITHUB_PASSWORD");
            if (string.IsNullOrEmpty(password))
            {
                return c.Failed("Can't find GITHUB_PASSWORD");
            }

            Cmd("git", "commit", "-m", "Updating dependencies", "--author", $"{userName} <{email}>")
                .Execute()
                .EnsureSuccessful();

            string remoteBranchName = $"UpdateDependencies-{DateTime.UtcNow.ToString("yyyyMMdd-hhmmss")}";
            Cmd("git", "push", $"https://{userName}:{password}@github.com/eerhardt/cli.git", $"HEAD:{remoteBranchName}")
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
