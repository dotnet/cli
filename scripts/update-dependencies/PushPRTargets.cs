using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Cli.Build.Framework;
using Octokit;

using static Microsoft.DotNet.Cli.Build.Framework.BuildHelpers;

namespace Microsoft.DotNet.Scripts
{
    public static class PushPRTargets
    {
        private const string PullRequestTitle = "Updating dependencies from last known good builds";

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

            Cmd("git", "commit", "-m", PullRequestTitle, "--author", $"{userName} <{email}>")
                .Execute()
                .EnsureSuccessful();

            string remoteBranchName = $"UpdateDependencies{DateTime.UtcNow.ToString("yyyyMMddhhmmss")}";
            Cmd("git", "push", $"https://{userName}:{password}@github.com/eerhardt/cli.git", $"HEAD:{remoteBranchName}")
                .Execute()
                .EnsureSuccessful();

            c.SetRemoteBranchName(remoteBranchName);

            return c.Success();
        }

        [Target]
        public static BuildTargetResult CreatePR(BuildTargetContext c)
        {
            string remoteBranchName = c.GetRemoteBranchName();

            NewPullRequest prInfo = new NewPullRequest(PullRequestTitle, "eerhardt"/*_config.GitHubOriginOwner*/ + ":" + remoteBranchName, "master" /*_config.GitHubUpstreamDestinationBranch*/);
            GitHubClient gitHub = new GitHubClient(new ProductHeaderValue("dotnetDependencyUpdater"));

            string password = Environment.GetEnvironmentVariable("GITHUB_PASSWORD");
            if (string.IsNullOrEmpty(password))
            {
                return c.Failed("Can't find GITHUB_PASSWORD");
            }
            gitHub.Credentials = new Credentials(password);

            gitHub.PullRequest.Create("eerhardt" /*_config.GitHubUpstreamOwner*/, "cli" /*_config.GitHubProject*/, prInfo).Wait();

            return c.Success();
        }

        private static string GetRemoteBranchName(this BuildTargetContext c)
        {
            return (string)c.BuildContext["RemoteBranchName"];
        }

        private static void SetRemoteBranchName(this BuildTargetContext c, string value)
        {
            c.BuildContext["RemoteBranchName"] = value;
        }
    }
}
