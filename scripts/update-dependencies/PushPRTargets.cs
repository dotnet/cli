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

            Cmd("git", "commit", "-m", PullRequestTitle, "--author", $"{userName} <{email}>")
                .EnvironmentVariable("GIT_COMMITTER_NAME", userName)
                .EnvironmentVariable("GIT_COMMITTER_EMAIL", email)
                .Execute()
                .EnsureSuccessful();

            string password = Environment.GetEnvironmentVariable("GITHUB_PASSWORD");
            if (string.IsNullOrEmpty(password))
            {
                return c.Failed("Can't find GITHUB_PASSWORD");
            }

            string remoteUrl = "github.com/eerhardt/cli.git"; // TODO: should be parameterized. this will eventually be "dotnet-bot/cli.git"
            string remoteBranchName = $"UpdateDependencies{DateTime.UtcNow.ToString("yyyyMMddhhmmss")}";
            string refSpec = $"HEAD:refs/heads/{remoteBranchName}";

            string logMessage = $"git push https://{remoteUrl} {refSpec}";
            BuildReporter.BeginSection("EXEC", logMessage);

            CommandResult pushResult =
                Cmd("git", "push", $"https://{userName}:{password}@{remoteUrl}", refSpec)
                    .QuietBuildReporter()  // we don't want secrets showing up in our logs
                    .CaptureStdErr() // git push will write to StdErr upon success, disable that
                    .CaptureStdOut()
                    .Execute();

            var message = logMessage + $" exited with {pushResult.ExitCode}";
            if (pushResult.ExitCode == 0)
            {
                BuildReporter.EndSection("EXEC", message.Green(), success: true);
            }
            else
            {
                BuildReporter.EndSection("EXEC", message.Red().Bold(), success: false);
            }

            pushResult.EnsureSuccessful();

            c.SetRemoteBranchName(remoteBranchName);

            return c.Success();
        }

        [Target]
        public static BuildTargetResult CreatePR(BuildTargetContext c)
        {
            string remoteBranchName = c.GetRemoteBranchName();

            NewPullRequest prInfo = new NewPullRequest(PullRequestTitle, "eerhardt"/*_config.GitHubOriginOwner*/ + ":" + remoteBranchName, "eerhardt/Test" /*_config.GitHubUpstreamDestinationBranch*/);
            GitHubClient gitHub = new GitHubClient(new ProductHeaderValue("dotnetDependencyUpdater"));

            string password = Environment.GetEnvironmentVariable("GITHUB_PASSWORD");
            if (string.IsNullOrEmpty(password))
            {
                return c.Failed("Can't find GITHUB_PASSWORD");
            }
            gitHub.Credentials = new Credentials(password);

            PullRequest createdPR = gitHub.PullRequest.Create("eerhardt" /*_config.GitHubUpstreamOwner*/, "cli" /*_config.GitHubProject*/, prInfo).Result;
            c.Info($"Created Pull Request: {createdPR.HtmlUrl}");

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
