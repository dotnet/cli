<Query Kind="Program">
  <NuGetReference>Octokit</NuGetReference>
  <NuGetReference>Octokit.Reactive</NuGetReference>
  <NuGetReference>Rx-Main</NuGetReference>
  <Namespace>Octokit</Namespace>
  <Namespace>Octokit.Reactive</Namespace>
  <Namespace>System</Namespace>
  <Namespace>System.Reactive.Linq</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

async Task Main(string[] args)
{
	var owner = string.Empty;
	var reponame = string.Empty;

	//Search Issues with xamarin keyword and get the results
	GitHubClient client = new GitHubClient(
		new Octokit.ProductHeaderValue("Octokit.samples"));
	owner = "octokit";
	reponame = "octokit.net";

	// or if you don't want to give an app your creds
	// you can use a token from an OAuth app
	// Here is the URL to get tokens https://github.com/settings/tokens
	// and save the token using Util.SetPassword("github","CHANGETHIS")
	client.Credentials = new Credentials(Util.GetPassword("github"));
	

	var issue = new SearchIssuesRequest("xamarin");
	issue.Repos.Add(owner,reponame);
	issue.SortField = IssueSearchSort.Updated;
	var searchresults = await client.Search.SearchIssues(issue);
	
	//For every issue get the comments for it
	var commentsclient = client.Issue.Comment;
	var comments = searchresults.Items.Select(async i =>
									new { IssueNumber = i.Number, 
										Comments = await commentsclient
											.GetAllForIssue(owner, reponame, i.Number)});
	
	var issueComments = await Task.WhenAll( comments);
	
	
	//Combine the comments with Issue and then dump it.
	searchresults.Items.Select(i => new
	{
		Number = Util.RawHtml(new XElement("a", 
				new XAttribute("href", i.HtmlUrl.ToString()), i.Number)),
		i.Title,
		i.Body,
		i.State,
		Comments = issueComments.FirstOrDefault(c => c.IssueNumber == i.Number)
					.Comments.Select(c => 
					new { User = c.User.Id, 
						  Name = c.User.Login,  	
						   Content = c.Body, 
						   Date = c.CreatedAt, 
						   Id = c.Id, c.Body})
	} ).Dump();
}